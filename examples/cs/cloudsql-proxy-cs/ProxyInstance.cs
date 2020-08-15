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

namespace cloudsql_proxy_cs
{
    /// <summary>
    /// Instance of a proxy that should be disposed of
    /// </summary>
    public class ProxyInstance: IDisposable
    {
        /// <summary>
        /// The current instance attached to this proxy instance
        /// </summary>
        public string Instance { get; }
        private Proxy Proxy { get; }

        // Track whether Dispose has been called.
        private bool disposed = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="proxy">Reference to main proxy class</param>
        /// <param name="instance">Proxy instance we started</param>
        public ProxyInstance(ref Proxy proxy, in string instance)
        {
            Instance = string.Copy(instance);
            Proxy = proxy;
        }

        /// <summary>
        /// Get the active port of this proxy instance
        /// </summary>
        /// <returns></returns>
        public int GetPort()
        {
            return Proxy?.GetPort(Instance) ?? 0;
        }

        /// <summary>
        /// Get the active port of this proxy instance
        /// </summary>
        /// <returns></returns>
        public Status GetStatus()
        {
            return Proxy?.GetStatus(Instance) ?? Status.Disconnected;
        }

        /// <summary>
        /// Dispose proxy instance
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// <list type="bullet">
        /// <item>If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.</item>
        /// <item>If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.</item>
        /// </list>
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                Proxy?.StopProxy(Instance);

                // Note disposing has been done.
                disposed = true;
            }
        }
    }
}