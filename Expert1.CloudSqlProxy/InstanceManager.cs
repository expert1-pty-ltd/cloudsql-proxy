using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Expert1.CloudSqlProxy
{
    /// <summary>
    /// Manages the lifecycle of ProxyInstance objects, ensuring that only one instance
    /// per unique database instance is active at a time. Handles creation, caching, and
    /// disposal of ProxyInstance objects.
    /// </summary>
    internal static class InstanceManager
    {
        private static readonly ConcurrentDictionary<string, (ProxyInstance instance, TaskCompletionSource<bool> connectionTaskSource, int refCount)> activeInstances = new();

        public static async Task<ProxyInstance> GetOrCreateInstanceAsync(AuthenticationMethod authenticationMethod, string instance, string credentials)
        {
            string key = instance;

            // Try to get an existing instance
            if (activeInstances.TryGetValue(key, out (ProxyInstance instance, TaskCompletionSource<bool> connectionTaskSource, int refCount) existingEntry))
            {
                Interlocked.Increment(ref existingEntry.refCount);
                await existingEntry.connectionTaskSource.Task; // Await until the connection is established or fails
                return existingEntry.instance;
            }

            // If no existing instance, create a new one
            TaskCompletionSource<bool> newConnectionTaskSource = new();
            ProxyInstance newInstance = new(authenticationMethod, instance, credentials);

            (ProxyInstance instance, TaskCompletionSource<bool> connectionTaskSource, int refCount) result = activeInstances.GetOrAdd(key, (newInstance, newConnectionTaskSource, 1));

            if (result.instance == newInstance)
            {
                // This means a new instance was created and we need to start it
                _ = newInstance.StartAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        newConnectionTaskSource.TrySetException(t.Exception);
                    }
                    else
                    {
                        newConnectionTaskSource.TrySetResult(true);
                    }
                });
            }

            await result.connectionTaskSource.Task; // Await until the connection is established or fails
            return result.instance;
        }

        public static void RemoveInstance(ProxyInstance instance)
        {
            string key = instance.Instance;
            if (activeInstances.TryGetValue(key, out var entry))
            {
                if (Interlocked.Decrement(ref entry.refCount) == 0)
                {
                    entry.instance.Stop();
                    activeInstances.TryRemove(key, out _);
                }
            }
        }

        public static void StopAllInstances()
        {
            foreach (string key in activeInstances.Keys)
            {
                if (activeInstances.TryRemove(key, out (ProxyInstance instance, TaskCompletionSource<bool> connectionTaskSource, int refCount) value))
                {
                    value.instance.Stop();
                }
            }
        }
    }
}
