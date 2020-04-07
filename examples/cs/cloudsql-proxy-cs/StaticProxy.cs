﻿// Copyright 2020 Expert 1 Pty Ltd. All Rights Reserved.
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

namespace cloudsql_proxy_cs
{
    /// <summary>
    /// Interface to the CloudSQL proxy Shared Library.
    /// </summary>
    public static class StaticProxy
    {
        /// <summary>
        /// Delegate which is passed to SetCallback on the go library
        /// </summary>
        /// <param name="status"></param>
        /// <param name="error"></param>
        internal delegate void StatusCallback(IntPtr status, IntPtr error);

        #region "Linux"
        [DllImport(@"cloud_sql_proxy.so", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "Echo")]
        internal extern static IntPtr EchoLinux(byte[] message);

        [DllImport(@"cloud_sql_proxy.so", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "StartProxyWithCredentialFile")]
        internal extern static void StartProxyWithCredentialFileLinux(byte[] instances, byte[] tokenFile);

        [DllImport(@"cloud_sql_proxy.so", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "StartProxyWithCredentialJson")]
        internal extern static void StartProxyWithCredentialJsonLinux(byte[] instances, byte[] tokenJson);

        [DllImport(@"cloud_sql_proxy.so", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "StopProxy")]
        internal extern static void StopProxyLinux();

        [DllImport(@"cloud_sql_proxy.so", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "GetStatus")]
        internal extern static IntPtr GetStatusLinux();

        [DllImport(@"cloud_sql_proxy.so", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "SetCallback")]
        internal extern static IntPtr SetCallbackLinux(StatusCallback cb);

        [DllImport(@"cloud_sql_proxy.so", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "GetPort")]
        internal extern static int GetPortLinux();
        #endregion

        #region "x64"
        [DllImport(@"cloud_sql_proxy_x64.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "Echo")]
        internal extern static IntPtr Echox64(byte[] message);

        [DllImport(@"cloud_sql_proxy_x64.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "StartProxyWithCredentialFile")]
        internal extern static void StartProxyWithCredentialFilex64(byte[] instances, byte[] tokenFile);

        [DllImport(@"cloud_sql_proxy_x64.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "StartProxyWithCredentialJson")]
        internal extern static void StartProxyWithCredentialJsonx64(byte[] instances, byte[] tokenJson);

        [DllImport(@"cloud_sql_proxy_x64.dll", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "StopProxy")]
        internal extern static void StopProxyx64();

        [DllImport(@"cloud_sql_proxy_x64.dll", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "GetStatus")]
        internal extern static IntPtr GetStatusx64();

        [DllImport(@"cloud_sql_proxy_x64.dll", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "SetCallback")]
        internal extern static IntPtr SetCallbackx64(StatusCallback cb);

        [DllImport(@"cloud_sql_proxy_x64.dll", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "GetPort")]
        internal extern static int GetPortx64();
        #endregion

        #region "x86"
        [DllImport(@"cloud_sql_proxy_x86.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "Echo")]
        internal extern static IntPtr Echox86(byte[] message);

        [DllImport(@"cloud_sql_proxy_x86.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "StartProxyWithCredentialJson")]
        internal extern static void StartProxyWithCredentialFilex86(byte[] instances, byte[] tokenFile);

        [DllImport(@"cloud_sql_proxy_x86.dll", CharSet = CharSet.Unicode,
           CallingConvention = CallingConvention.StdCall, EntryPoint = "StartProxyWithCredentialFile")]
        internal extern static void StartProxyWithCredentialJsonx86(byte[] instances, byte[] tokenJson);

        [DllImport(@"cloud_sql_proxy_x86.dll", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "StopProxy")]
        internal extern static void StopProxyx86();

        [DllImport(@"cloud_sql_proxy_x86.dll", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "GetStatus")]
        internal extern static IntPtr GetStatusx86();

        [DllImport(@"cloud_sql_proxy_x86.dll", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "SetCallback")]
        internal extern static IntPtr SetCallbackx86(StatusCallback cb);

        [DllImport(@"cloud_sql_proxy_x86.dll", CharSet = CharSet.Unicode,
                   CallingConvention = CallingConvention.StdCall, EntryPoint = "GetPort")]
        internal extern static int GetPortx86();
        #endregion
    }
}