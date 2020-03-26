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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            var instance = args.Length == 1 ? args[0] : "";
            var tokenFile = args.Length == 2 ? args[1] : @"google_cloud_key.json";

            if (string.IsNullOrWhiteSpace(instance))
            {
                NonBlockingConsole.WriteError("Usage: cmd instance [tokenFile]");
            }

            // Create the token source.
            using (var cts = new CancellationTokenSource())
            {
                // Pass the token to the cancelable operation.
                ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), new object[] { cts.Token, instance, tokenFile});

                NonBlockingConsole.WriteLine("Type :quit to exit");
                var input = Console.ReadLine().ToLower().Trim();
                while (input != ":quit" && !cts.IsCancellationRequested)
                {
                    switch (input)
                    {
                        case "stop":
                            cts.Cancel();
                            cloudsql_proxy_cs.Proxy.StopProxy();
                            break;
                        default:
                            NonBlockingConsole.WriteLine("Type :quit to exit");
                            break;
                    }
                    input = Console.ReadLine().ToLower().Trim();
                }

                NonBlockingConsole.WriteDebug("Shutting down thread");
                cts.Cancel();
                NonBlockingConsole.WriteDebug("Good bye");
            }
        }

        static void DoWork(object obj)
        {
            CancellationToken token = (CancellationToken)((object[])obj)[0];
            string instance = (string)((object[])obj)[1];
            string tokenFile  = (string)((object[])obj)[2];

            if (!token.IsCancellationRequested)
            {
                using (var proxy = new cloudsql_proxy_cs.Proxy())
                {
                    NonBlockingConsole.WriteLine("Testing connection to CloudSQL Proxy Library");
                    var ret = proxy.Echo("test");
                    if (ret == "From DLL: test")
                    {
                        NonBlockingConsole.WriteSuccess("Test Passed");
                    }

                    proxy.StartProxy(instance, tokenFile);
                }
            }
            else
            {
                NonBlockingConsole.WriteLine("Stopping thread");
            }
        }
    }
}
