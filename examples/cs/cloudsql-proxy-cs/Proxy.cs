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
    public class Proxy: IDisposable
    {
        [DllImport(@"cloud_sql_proxy.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall)]
        private extern static IntPtr Echo(byte[] message);

        [DllImport(@"cloud_sql_proxy.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall)]
        private extern static void StartProxy(byte[] instances, byte[] tokenFile);

        [DllImport(@"cloud_sql_proxy.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall)]
        public extern static void StopProxy();

        public Proxy()
        {
        }

        public string Echo(string message)
        {
            return Marshal.PtrToStringAnsi(Echo((Encoding.UTF8.GetBytes(message))));
        }

        public void StartProxy(string instances, string tokenFile)
        {
            StartProxy(Encoding.UTF8.GetBytes(instances), Encoding.UTF8.GetBytes(tokenFile));
        }

        public void Dispose()
        {
            StopProxy();
        }
    }
}
