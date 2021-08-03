// Copyright 2020 Expert 1 Pty Ltd. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace cloudsql_proxy_cs
{
    /// <summary>
    /// Class used for the management of the Cloud SQL Proxy.
    /// </summary>
    public sealed class Proxy : IDisposable
    {
        private const string StatusConnecting = "connecting";
        private const string StatusConnected = "connected";
        private const string StatusError = "error";
        private const string Platform_Linux_Arm64 = "linux-arm64";
        private const string Platform_Linux_64 = "linux-64";
        private const string Platform_Win_64 = "win-64";
        private const string Platform_Win_32 = "win-32";
        private const string Invalid_Platform = "Invalid platform";
        private static Proxy _instance;

        private string Platform
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    switch (RuntimeInformation.OSArchitecture)
                    {
                        case Architecture.Arm64:
                            return Platform_Linux_Arm64;
                        case Architecture.X64:
                            return Platform_Linux_64;
                        default:
                            return "";
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return Platform_Linux_64;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    switch (RuntimeInformation.OSArchitecture)
                    {
                        case Architecture.X64:
                            return Platform_Win_64;
                        case Architecture.X86:
                            return Platform_Win_32;
                        default:
                            return "";
                    }
                }
                else
                {
                    return "";
                }
            }
        }

        private bool disposed;

        private static StaticProxy.StatusCallback statusCallbackReference;
        private static StaticProxy.StatusCallbackLinux statusCallbackReferenceLinux;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> tcss;
        private readonly ConcurrentDictionary<string, int> proxyCounter;

        /// <summary>
        /// Triggers when the <see cref="Status"/> of the Proxy changes.
        /// Can be used to capture the status as it moves through different states.
        /// </summary>
        public event EventHandler<StatusEventArgs> OnStatusChanged;

        /// <summary>
        /// Triggers when the <see cref="Status"/> of the Proxy changes to Connected.
        /// </summary>
        public event EventHandler<string> OnConnected;

        /// <summary>
        /// Triggers when the <see cref="Status"/> of the Proxy changes to Disconnected.
        /// </summary>
        public event EventHandler<string> OnDisconnected;

        /// <summary>
        /// Triggers when the <see cref="Status"/> of the Proxy changes to Error.
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnError;

        /// <summary>
        /// Is proxy connecting
        /// </summary>
        public bool IsConnecting(string instances)
        {
            return GetStatus(instances) == Status.Connecting;
        }

        /// <summary>
        /// Is proxy connected
        /// </summary>
        public bool IsConnected(string instances)
        {
            return GetStatus(instances) == Status.Connected;
        }

        /// <summary>
        /// Is proxy disconnected
        /// </summary>
        public bool IsDisconnected(string instances)
        {
            return GetStatus(instances) == Status.Disconnected;
        }

        /// <summary>
        /// Default constructor - must start proxy manually.
        /// </summary>
        private Proxy()
        {
            tcss = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
            proxyCounter = new ConcurrentDictionary<string, int>();
        }

        /// <summary>
        /// Get static instance of the proxy
        /// </summary>
        public static Proxy GetInstance()
        {
            return _instance ?? (_instance = new Proxy());
        }

        /// <summary>
        /// Start the proxy manually. This method will block until the proxy is connected.
        /// </summary>
        /// <param name="authenticationMethod">authentication method</param>
        /// <param name="instance">instance; to bind any available port use port 0. You can find the port number using GetPort()</param>
        /// <param name="credentials">credential file or json</param>
        public async Task<ProxyInstance> StartProxyAsync(AuthenticationMethod authenticationMethod, string instance, string credentials)
        {
            var proxyInstance = new ProxyInstance(ref _instance, instance);

            if (tcss.ContainsKey(instance))
            {
                var rr = await tcss[instance].Task.ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(rr))
                {
                    throw new Exception(rr);
                }
                proxyCounter[instance]++;
                return proxyInstance;
            }

            tcss.TryAdd(instance, new TaskCompletionSource<string>());
            proxyCounter.TryAdd(instance, 1);
            var thread = new Thread(RunJob);
            thread.Start(new JobParams()
            {
                Platform = string.Copy(Platform),
                AuthenticationMethod = authenticationMethod,
                Instance = string.Copy(instance),
                Credentials = string.Copy(credentials)
            });

            var result = await tcss[instance].Task.ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(result))
            {
                throw new Exception(result);
            }
            return proxyInstance;
        }

        /// <summary>
        /// Start the proxy manually. This method will block until the proxy is connected.
        /// </summary>
        /// <param name="authenticationMethod">authentication method</param>
        /// <param name="instance">instance; to bind any available port use port 0. You can find the port number using GetPort()</param>
        /// <param name="credentials">credential file or json</param>
        public ProxyInstance StartProxy(AuthenticationMethod authenticationMethod, string instance, string credentials)
        {
            var proxyInstance = new ProxyInstance(ref _instance, instance);

            Task<string> task;
            string result;

            if (tcss.ContainsKey(instance))
            {
                task = tcss[instance].Task;
                task.Wait();
                result = task.Result;
                if (!string.IsNullOrWhiteSpace(result))
                {
                    throw new Exception(result);
                }
                proxyCounter[instance]++;
                return proxyInstance;
            }

            tcss.TryAdd(instance, new TaskCompletionSource<string>());
            var thread = new Thread(RunJob);
            proxyCounter.TryAdd(instance, 1);
            thread.Start(new JobParams()
            {
                Platform = string.Copy(Platform),
                AuthenticationMethod = authenticationMethod,
                Instance = string.Copy(instance),
                Credentials = string.Copy(credentials)
            });

            task = tcss[instance].Task;
            task.Wait();
            result = task.Result;
            if (!string.IsNullOrWhiteSpace(result))
            {
                throw new Exception(result);
            }
            return proxyInstance;
        }

        /// <summary>
        /// Implements SetStatus delegate which is passed into SetCallback on the go library interface.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="status"></param>
        /// <param name="error"></param>
        internal void SetStatus(string instance, string status, string error)
        {
            // decode message from bytes
            //var instanceStr = Marshal.PtrToStringAnsi(instance);
            //var statusStr = Marshal.PtrToStringAnsi(status);
            //var errorStr = Marshal.PtrToStringAnsi(error);

            switch (status)
            {
                case StatusConnecting:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = instance,
                        Status = Status.Connecting
                    });
                    break;
                case StatusConnected:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = instance,
                        Status = Status.Connected
                    });
                    OnConnected?.Invoke(this, instance);
                    if (tcss.ContainsKey(instance))
                    {
                        tcss[instance]?.TrySetResult("");
                    }
                    break;
                case StatusError:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = instance,
                        Status = Status.Error
                    });
                    OnError?.Invoke(this, new ErrorEventArgs()
                    {
                        Instance = instance,
                        ErrorMessage = error
                    });
                    if (tcss.ContainsKey(instance))
                    {
                        tcss[instance]?.TrySetResult(error);
                    }
                    break;
                default:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = instance,
                        Status = Status.Disconnected
                    });
                    OnDisconnected?.Invoke(this, instance);
                    break;
            }
        }

        /// <summary>
        /// Echo from the go library, used to test the go library connectivity.
        /// </summary>
        /// <param name="message">message to send</param>
        public string Echo(string message)
        {
            switch (Platform)
            {
                case Platform_Linux_Arm64:
                    return Marshal.PtrToStringAnsi(StaticProxy.EchoLinuxArm64(Encoding.UTF8.GetBytes(message)));
                case Platform_Linux_64:
                    return Marshal.PtrToStringAnsi(StaticProxy.EchoLinux(Encoding.UTF8.GetBytes(message)));
                case Platform_Win_64:
                    return Marshal.PtrToStringAnsi(StaticProxy.Echox64(Encoding.UTF8.GetBytes(message)));
                case Platform_Win_32:
                    return Marshal.PtrToStringAnsi(StaticProxy.Echox86(Encoding.UTF8.GetBytes(message)));
                default:
                    throw new Exception(Invalid_Platform);
            }
        }

        /// <summary>
        /// Get the port number that the proxy is listening on.
        /// </summary>
        /// <exception cref="ProxyNotConnectedException">
        /// Thrown when function is called and proxy is not connected.
        /// </exception>
        /// <exception cref="InvalidProxyException">
        /// Thrown when function is called and proxy has not been started.
        /// </exception>
        /// <returns>Port number</returns>
        public int GetPort(string instances)
        {
            if (!tcss.ContainsKey(instances))
            {
                throw new InvalidProxyException($"The proxy instance {instances} has not been started");
            }

            if (GetStatus(instances) == Status.Connected)
            {
                switch (Platform)
                {
                    case Platform_Linux_Arm64:
                        return StaticProxy.GetPortLinuxArm64(Encoding.UTF8.GetBytes(instances));
                    case Platform_Linux_64:
                        return StaticProxy.GetPortLinux(Encoding.UTF8.GetBytes(instances));
                    case Platform_Win_64:
                        return StaticProxy.GetPortx64(Encoding.UTF8.GetBytes(instances));
                    case Platform_Win_32:
                        return StaticProxy.GetPortx86(Encoding.UTF8.GetBytes(instances));
                    default:
                        throw new Exception(Invalid_Platform);
                }
            }
            else
            {
                // not connected yet, so port is unallocated.
                throw new ProxyNotConnectedException();
            }
        }

        /// <summary>
        /// Stops the Proxy.
        /// <exception cref="ProxyNotConnectedException">
        /// Thrown when function is called and proxy is not connected.
        /// </exception>
        /// <exception cref="InvalidProxyException">
        /// Thrown when function is called and proxy has not been started.
        /// </exception>
        /// </summary>
        public void StopProxy(string instances)
        {
            if (!tcss.ContainsKey(instances))
            {
                return;
            }

            // only shut down once we know we don't have any more connections
            if (proxyCounter[instances] > 1)
            {
                proxyCounter[instances]--;
            }
            else
            {
                if (GetStatus(instances) == Status.Connected)
                {
                    switch (Platform)
                    {
                        case Platform_Linux_Arm64:
                            StaticProxy.StopProxyLinuxArm64(Encoding.UTF8.GetBytes(instances));
                            break;
                        case Platform_Linux_64:
                            StaticProxy.StopProxyLinux(Encoding.UTF8.GetBytes(instances));
                            break;
                        case Platform_Win_64:
                            StaticProxy.StopProxyx64(Encoding.UTF8.GetBytes(instances));
                            break;
                        case Platform_Win_32:
                            StaticProxy.StopProxyx86(Encoding.UTF8.GetBytes(instances));
                            break;
                        default:
                            throw new Exception(Invalid_Platform);
                    }

                    proxyCounter.TryRemove(instances, out _);
                    _ = tcss.TryRemove(instances, out _);
                }
                else
                {
                    // not connected yet, so port is unallocated.
                    throw new ProxyNotConnectedException();
                }
            }
        }

        /// <summary>
        /// Stops all proxies.
        /// </summary>
        public void StopAll()
        {
            switch (Platform)
            {
                case Platform_Linux_Arm64:
                    StaticProxy.StopAllLinuxArm64();
                    break;
                case Platform_Linux_64:
                    StaticProxy.StopAllLinux();
                    break;
                case Platform_Win_64:
                    StaticProxy.StopAllx64();
                    break;
                case Platform_Win_32:
                    StaticProxy.StopAllx86();
                    break;
                default:
                    throw new Exception(Invalid_Platform);
            }

            proxyCounter.Clear();
            tcss.Clear();
        }

        /// <summary>
        /// Gets the <see cref="Status"/> of the Proxy.
        /// </summary>
        /// <exception cref="InvalidProxyException"></exception>
        public Status GetStatus(string instances)
        {
            if (!tcss.ContainsKey(instances))
            {
                throw new InvalidProxyException($"The proxy instance {instances} has not been started");
            }

            int status;
            switch (Platform)
            {
                case Platform_Linux_Arm64:
                    status = StaticProxy.GetStatusLinuxArm64(Encoding.UTF8.GetBytes(instances));
                    break;
                case Platform_Linux_64:
                    status = StaticProxy.GetStatusLinux(Encoding.UTF8.GetBytes(instances));
                    break;
                case Platform_Win_64:
                    status = StaticProxy.GetStatusx64(Encoding.UTF8.GetBytes(instances));
                    break;
                case Platform_Win_32:
                    status = StaticProxy.GetStatusx86(Encoding.UTF8.GetBytes(instances));
                    break;
                default:
                    throw new Exception(Invalid_Platform);
            }

            return (Status)status;
        }

        private void RunJob(object p)
        {
            var jobParam = (JobParams)p;

            switch (jobParam.AuthenticationMethod)
            {
                case AuthenticationMethod.CredentialFile:
                    StartProxyWithCredentialFile(jobParam.Platform, jobParam.Instance, jobParam.Credentials);
                    break;
                case AuthenticationMethod.JSON:
                    StartProxyWithCredentialJson(jobParam.Platform, jobParam.Instance, jobParam.Credentials);
                    break;
            }
        }

        private void StartProxyWithCredentialFile(string platform, string instances, string tokenFile)
        {
            switch (platform)
            {
                case Platform_Linux_Arm64:
                    statusCallbackReferenceLinux = new StaticProxy.StatusCallbackLinux(SetStatus);
                    StaticProxy.StartProxyWithCredentialFileLinuxArm64(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile), statusCallbackReferenceLinux);
                    break;
                case Platform_Linux_64:
                    statusCallbackReferenceLinux = new StaticProxy.StatusCallbackLinux(SetStatus);
                    StaticProxy.StartProxyWithCredentialFileLinux(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile), statusCallbackReferenceLinux);
                    break;
                case Platform_Win_64:
                    statusCallbackReference = new StaticProxy.StatusCallback(SetStatus);
                    StaticProxy.StartProxyWithCredentialFilex64(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile), statusCallbackReference);
                    break;
                case Platform_Win_32:
                    statusCallbackReference = new StaticProxy.StatusCallback(SetStatus);
                    StaticProxy.StartProxyWithCredentialFilex86(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile), statusCallbackReference);
                    break;
                default:
                    throw new Exception(Invalid_Platform);
            }
        }

        private void StartProxyWithCredentialJson(string platform, string instances, string tokenJson)
        {
            switch (platform)
            {
                case Platform_Linux_Arm64:
                    statusCallbackReferenceLinux = new StaticProxy.StatusCallbackLinux(SetStatus);
                    StaticProxy.StartProxyWithCredentialJsonLinuxArm64(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenJson), statusCallbackReferenceLinux);
                    break;
                case Platform_Linux_64:
                    statusCallbackReferenceLinux = new StaticProxy.StatusCallbackLinux(SetStatus);
                    StaticProxy.StartProxyWithCredentialJsonLinux(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenJson), statusCallbackReferenceLinux);
                    break;
                case Platform_Win_64:
                    statusCallbackReference = new StaticProxy.StatusCallback(SetStatus);
                    StaticProxy.StartProxyWithCredentialJsonx64(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenJson), statusCallbackReference);
                    break;
                case Platform_Win_32:
                    statusCallbackReference = new StaticProxy.StatusCallback(SetStatus);
                    StaticProxy.StartProxyWithCredentialJsonx86(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenJson), statusCallbackReference);
                    break;
                default:
                    throw new Exception(Invalid_Platform);
            }
        }

        private void SetStatus(IntPtr instance, IntPtr status, IntPtr error)
        {
            // decode message from bytes
            var instanceStr = Marshal.PtrToStringAnsi(instance);
            var statusStr = Marshal.PtrToStringAnsi(status);
            var errorStr = Marshal.PtrToStringAnsi(error);

            switch (statusStr)
            {
                case StatusConnecting:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = instanceStr,
                        Status = Status.Connecting
                    });
                    break;
                case StatusConnected:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = instanceStr,
                        Status = Status.Connected
                    });
                    OnConnected?.Invoke(this, instanceStr);
                    if (tcss.ContainsKey(instanceStr))
                    {
                        tcss[instanceStr]?.TrySetResult("");
                    }
                    break;
                case StatusError:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = instanceStr,
                        Status = Status.Error
                    });
                    OnError?.Invoke(this, new ErrorEventArgs()
                    {
                        Instance = instanceStr,
                        ErrorMessage = errorStr
                    });
                    if (tcss.ContainsKey(instanceStr))
                    {
                        tcss[instanceStr]?.TrySetResult(errorStr);
                    }
                    break;
                default:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = instanceStr,
                        Status = Status.Disconnected
                    });
                    OnDisconnected?.Invoke(this, instanceStr);
                    break;
            }
        }

        ///<inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            if (!disposed)
            {
                // instruct proxy to die
                StopAll();

                // Note disposing has been done.
                disposed = true;
            }
        }
    }
}
