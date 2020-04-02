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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace cloudsql_proxy_cs
{
    public class Proxy : IDisposable
    {
        private string Platform
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        return "linux-64";
                    case PlatformID.Win32NT:
                        {
                            if (Environment.Is64BitProcess)
                            {
                                return "win-64";
                            }
                            else
                            {
                                return "win-32";
                            }
                        }
                }
                return "win-64";
            }
        }

        private AuthenticationMethod AuthenticationMethod { get; set; }
        private string Instance { get; set; }
        private string Credentials { get; set; }

        private Thread job;

        public EventHandler<Status> OnStatusChanged { get; set; }
        public EventHandler OnConnected { get; set; }
        public EventHandler OnDisconnected { get; set; }

        /// <summary>
        /// Get Status of Proxy
        /// </summary>
        public Status Status
        {
            get
            {
                var status = "";
                switch (Platform)
                {
                    case "linux-64":
                        status = Marshal.PtrToStringAnsi(StaticProxy.GetStatusLinux());
                        break;
                    case "win-64":
                        status = Marshal.PtrToStringAnsi(StaticProxy.GetStatusx64());
                        break;
                    case "win-32":
                        status = Marshal.PtrToStringAnsi(StaticProxy.GetStatusx86());
                        break;
                    default:
                        throw new Exception("Invalid platform");
                }

                switch (status)
                {
                    case "connecting":
                        return Status.Connecting;
                    case "connected":
                        return Status.Connected;
                    default:
                        return Status.Disconnected;
                }
            }
        }

        /// <summary>
        /// Is proxy connecting
        /// </summary>
        public bool IsConnecting
        {
            get
            {
                return Status == Status.Connecting;
            }
        }

        /// <summary>
        /// Is proxy connected
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return Status == Status.Connected;
            }
        }

        /// <summary>
        /// Is proxy disconnected
        /// </summary>
        public bool IsDisconnected
        {
            get
            {
                return Status == Status.Disconnected;
            }
        }

        /// <summary>
        /// Default constructor - must start proxy manually
        /// </summary>
        public Proxy()
        {
        }

        /// <summary>
        /// Contruct and start proxy
        /// </summary>
        /// <param name="authenticationMethod">authentication method</param>
        /// <param name="instance">instance</param>
        /// <param name="credentials">credential file or json</param>
        public Proxy(AuthenticationMethod authenticationMethod, string instance, string credentials)
        {
            this.StartProxy(authenticationMethod, instance, credentials);
        }

        /// <summary>
        /// Start the proxy manually
        /// </summary>
        /// <param name="authenticationMethod">authentication method</param>
        /// <param name="instance">instance</param>
        /// <param name="credentials">credential file or json</param>
        public void StartProxy(AuthenticationMethod authenticationMethod, string instance, string credentials)
        {
            AuthenticationMethod = authenticationMethod;
            Instance = instance;
            Credentials = credentials;

            StaticProxy.SetCallbackx64(SetStatus);

            job = new Thread(new ThreadStart(RunJob));
            job.Start();
        }

        /// <summary>
        /// Implements SetStatus delegate which is passed into SetCallback on the go library interface
        /// </summary>
        /// <param name="status"></param>
        private void SetStatus(IntPtr status)
        {
            // decode message from bytes
            var statusStr = Marshal.PtrToStringAnsi(status);
            switch (statusStr)
            {
                case "connecting":
                    OnStatusChanged?.Invoke(this, Status.Connecting);
                    break;
                case "connected":
                    OnStatusChanged?.Invoke(this, Status.Connected);
                    OnConnected?.Invoke(this, null);
                    break;
                default:
                    OnStatusChanged?.Invoke(this, Status.Disconnected);
                    OnDisconnected?.Invoke(this, null);
                    break;
            }
        }

        /// <summary>
        /// Echo from the go library - test connectivity
        /// </summary>
        /// <param name="message">message to send</param>
        /// <returns></returns>
        public string Echo(string message)
        {
            switch (Platform)
            {
                case "linux-64":
                    return Marshal.PtrToStringAnsi(StaticProxy.EchoLinux((Encoding.UTF8.GetBytes(message))));
                case "win-64":
                    return Marshal.PtrToStringAnsi(StaticProxy.Echox64((Encoding.UTF8.GetBytes(message))));
                case "win-32":
                    return Marshal.PtrToStringAnsi(StaticProxy.Echox86((Encoding.UTF8.GetBytes(message))));
                default:
                    throw new Exception("Invalid platform");
            }
        }

        /// <summary>
        /// Stop proxy
        /// </summary>
        public void StopProxy()
        {
            switch (Platform)
            {
                case "linux-64":
                    StaticProxy.StopProxyLinux();
                    break;
                case "win-64":
                    StaticProxy.StopProxyx64();
                    break;
                case "win-32":
                    StaticProxy.StopProxyx86();
                    break;
                default:
                    throw new Exception("Invalid platform");
            }

            // wait for proxy to die
            job.Join(1000);
        }

        private void RunJob()
        {
            switch (AuthenticationMethod)
            {
                case AuthenticationMethod.CredentialFile:
                    StartProxyWithCredentialFile(Instance, Credentials);
                    break;
                case AuthenticationMethod.JSON:
                    StartProxyWithCredentialJson(Instance, Credentials);
                    break;
            }
        }

        private void StartProxyWithCredentialFile(string instances, string tokenFile)
        {
            switch (Platform)
            {
                case "linux-64":
                    StaticProxy.StartProxyWithCredentialFileLinux(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile));
                    break;
                case "win-64":
                    StaticProxy.StartProxyWithCredentialFilex64(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile));
                    break;
                case "win-32":
                    StaticProxy.StartProxyWithCredentialFilex86(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile));
                    break;
                default:
                    throw new Exception("Invalid platform");
            }
        }

        private void StartProxyWithCredentialJson(string instances, string tokenJson)
        {
            switch (Platform)
            {
                case "linux-64":
                    StaticProxy.StartProxyWithCredentialJsonLinux(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenJson));
                    break;
                case "win-64":
                    StaticProxy.StartProxyWithCredentialJsonx64(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenJson));
                    break;
                case "win-32":
                    StaticProxy.StartProxyWithCredentialJsonx86(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenJson));
                    break;
                default:
                    throw new Exception("Invalid platform");
            }
        }

        public void Dispose()
        {
            // instruct proxy to die
            StopProxy();
        }
    }
}
