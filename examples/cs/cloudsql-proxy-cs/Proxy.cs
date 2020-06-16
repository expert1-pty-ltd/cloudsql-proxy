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

using Grpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace cloudsql_proxy_cs
{
    /// <summary>
    /// Class used for the management of the Cloud SQL Proxy.
    /// </summary>
    public class Proxy : IDisposable
    {
        private static Proxy _instance;

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
                return "";
            }
        }

        //private static StaticProxy.StatusCallback statusCallbackReference;

        private ConcurrentDictionary<string, TaskCompletionSource<string>> tcss;
        private ConcurrentDictionary<string, int> proxyCounter;

        /// <summary>
        /// Triggers when the <see cref="cloudsql_proxy_cs.Status"/> of the Proxy changes.
        /// Can be used to capture the status as it moves through different states.
        /// </summary>
        public event EventHandler<StatusEventArgs> OnStatusChanged;

        /// <summary>
        /// Triggers when the <see cref="cloudsql_proxy_cs.Status"/> of the Proxy changes to Connected.
        /// </summary>
        public event EventHandler<string> OnConnected;

        /// <summary>
        /// Triggers when the <see cref="cloudsql_proxy_cs.Status"/> of the Proxy changes to Disconnected.
        /// </summary>
        public event EventHandler<string> OnDisconnected;

        /// <summary>
        /// Triggers when the <see cref="cloudsql_proxy_cs.Status"/> of the Proxy changes to Error.
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

            var server = new Server
            {
                Services = { ProxyStatus.BindService(new ProxyStatusImpl()) },
                Ports = { new ServerPort("localhost", 50001, ServerCredentials.Insecure) }
            };

            //statusCallbackReference = new StaticProxy.StatusCallback(SetStatus);

            //switch (Platform)
            //{
            //    case "linux-64":
            //        StaticProxy.SetCallbackLinux(statusCallbackReference);
            //        break;
            //    case "win-64":
            //        StaticProxy.SetCallbackx64(statusCallbackReference);
            //        break;
            //    case "win-32":
            //        StaticProxy.SetCallbackx86(statusCallbackReference);
            //        break;
            //    default:
            //        throw new Exception("Invalid platform");
            //}
        }

        /// <summary>
        /// Get static instance of the proxy
        /// </summary>
        /// <returns></returns>
        public static Proxy GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Proxy();
            }
            return _instance;
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
                var rr = await tcss[instance].Task;
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

            var result = await tcss[instance].Task;
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
                case "connecting":
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = instance,
                        Status = Status.Connecting
                    });
                    break;
                case "connected":
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
                case "error":
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
        /// Implements SetConnected delegate which is passed into SetCallback on the go library interface.
        /// </summary>
        /// <param name="success"></param>
        private void SetConnected(int success)
        {
            switch (success)
            {
                case 0:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = "all",
                        Status = Status.Connecting
                    });
                    break;
                case 1:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = "all",
                        Status = Status.Connected
                    });
                    OnConnected?.Invoke(this, "all");
                    foreach (var key in tcss.Keys)
                    {
                        tcss[key]?.TrySetResult("");
                    }
                    break;
                case 2:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = "all",
                        Status = Status.Error
                    });
                    OnError?.Invoke(this, new ErrorEventArgs()
                    {
                        Instance = "all",
                        ErrorMessage = "An error occured"
                    });
                    foreach (var key in tcss.Keys)
                    {
                        tcss[key]?.TrySetResult("An error occured");
                    }
                    break;
                default:
                    OnStatusChanged?.Invoke(this, new StatusEventArgs()
                    {
                        Instance = "all",
                        Status = Status.Disconnected
                    });
                    OnDisconnected?.Invoke(this, "all");
                    break;
            }
        }

        /// <summary>
        /// Echo from the go library, used to test the go library connectivity.
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
                    case "linux-64":
                        return StaticProxy.GetPortLinux(Encoding.UTF8.GetBytes(instances));
                    case "win-64":
                        return StaticProxy.GetPortx64(Encoding.UTF8.GetBytes(instances));
                    case "win-32":
                        return StaticProxy.GetPortx86(Encoding.UTF8.GetBytes(instances));
                    default:
                        throw new Exception("Invalid platform");
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
                        case "linux-64":
                            StaticProxy.StopProxyLinux(Encoding.UTF8.GetBytes(instances));
                            break;
                        case "win-64":
                            StaticProxy.StopProxyx64(Encoding.UTF8.GetBytes(instances));
                            break;
                        case "win-32":
                            StaticProxy.StopProxyx86(Encoding.UTF8.GetBytes(instances));
                            break;
                        default:
                            throw new Exception("Invalid platform");
                    }

                    proxyCounter.TryRemove(instances, out int outVal1);
                    tcss.TryRemove(instances, out TaskCompletionSource<string> outVal3);
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
                case "linux-64":
                    StaticProxy.StopAllLinux();
                    break;
                case "win-64":
                    StaticProxy.StopAllx64();
                    break;
                case "win-32":
                    StaticProxy.StopAllx86();
                    break;
                default:
                    throw new Exception("Invalid platform");
            }

            proxyCounter.Clear();
            tcss.Clear();
        }

        /// <summary>
        /// Gets the <see cref="cloudsql_proxy_cs.Status"/> of the Proxy.
        /// </summary>
        public Status GetStatus(string instances)
        {
            if (!tcss.ContainsKey(instances))
            {
                throw new InvalidProxyException($"The proxy instance {instances} has not been started");
            }

            string status;
            switch (Platform)
            {
                case "linux-64":
                    status = Marshal.PtrToStringAnsi(StaticProxy.GetStatusLinux(Encoding.UTF8.GetBytes(instances)));
                    break;
                case "win-64":
                    status = Marshal.PtrToStringAnsi(StaticProxy.GetStatusx64(Encoding.UTF8.GetBytes(instances)));
                    break;
                case "win-32":
                    status = Marshal.PtrToStringAnsi(StaticProxy.GetStatusx86(Encoding.UTF8.GetBytes(instances)));
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
                case "error":
                    return Status.Error;
                default:
                    return Status.Disconnected;
            }
        }

        private void RunJob(object p)
        {
            var jobParam = ((JobParams)p);

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

        private void StartProxyWithCredentialJson(string platform, string instances, string tokenJson)
        {
            switch (platform)
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

        ///<inheritdoc cref="IDisposable"/> 
        public void Dispose()
        {
            // instruct proxy to die
            StopAll();
        }
    }
}
