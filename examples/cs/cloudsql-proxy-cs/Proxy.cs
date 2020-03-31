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

namespace cloudsql_proxy_cs
{
    public class Proxy : IDisposable
    {
        #region "Linux"
        [DllImport(@"cloud_sql_proxy.so", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "Echo")]
        private extern static IntPtr EchoLinux(byte[] message);

        [DllImport(@"cloud_sql_proxy.so", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "StartProxy")]
        private extern static void StartProxyLinux(byte[] instances, byte[] tokenFile);

        [DllImport(@"cloud_sql_proxy.so", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "StopProxy")]
        private extern static void StopProxyLinux();
        #endregion

        #region "x64"
        [DllImport(@"cloud_sql_proxy_x64.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "Echo")]
        private extern static IntPtr Echox64(byte[] message);

        [DllImport(@"cloud_sql_proxy_x64.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "StartProxy")]
        private extern static void StartProxyx64(byte[] instances, byte[] tokenFile);

        [DllImport(@"cloud_sql_proxy_x64.dll", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "StopProxy")]
        private extern static void StopProxyx64();
        #endregion

        #region "x86"
        [DllImport(@"cloud_sql_proxy_x86.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "Echo")]
        private extern static IntPtr Echox86(byte[] message);

        [DllImport(@"cloud_sql_proxy_x86.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "StartProxy")]
        private extern static void StartProxyx86(byte[] instances, byte[] tokenFile);

        [DllImport(@"cloud_sql_proxy_x86.dll", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "StopProxy")]
        private extern static void StopProxyx86();
        #endregion

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

        public Proxy()
        {
        }

        public string Echo(string message)
        {
            switch (Platform)
            {
                case "linux-64":
                    return Marshal.PtrToStringAnsi(EchoLinux((Encoding.UTF8.GetBytes(message))));
                case "win-64":
                    return Marshal.PtrToStringAnsi(Echox64((Encoding.UTF8.GetBytes(message))));
                case "win-32":
                    return Marshal.PtrToStringAnsi(Echox86((Encoding.UTF8.GetBytes(message))));
                default:
                    throw new Exception("Invalid platform");
            }
        }

        public void StartProxy(string instances, string tokenFile)
        {
            switch (Platform)
            {
                case "linux-64":
                    StartProxyLinux(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile));
                    break;
                case "win-64":
                    StartProxyx64(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile));
                    break;
                case "win-32":
                    StartProxyx86(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile));
                    break;
                default:
                    throw new Exception("Invalid platform");
            }
        }

        public void StopProxy()
        {
            switch (Platform)
            {
                case "linux-64":
                    StopProxyLinux();
                    break;
                case "win-64":
                    StopProxyx64();
                    break;
                case "win-32":
                    StopProxyx86();
                    break;
                default:
                    throw new Exception("Invalid platform");
            }
        }

        public void Dispose()
        {
            StopProxy();
        }
    }
}
