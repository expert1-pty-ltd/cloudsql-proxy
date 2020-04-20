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
using System.Threading;

namespace cmd
{
    class Program
    {
        private static bool exit = false;

        static void Main(string[] args)
        {
            //var instance = args.Length == 1 ? args[0] : "";
            var tokenFile = args.Length == 2 ? args[1] : @"google_cloud_key.json";

            // Validate Instance
            //if (string.IsNullOrWhiteSpace(instance))
            //{
            //    NonBlockingConsole.WriteError("Usage: cmd instance [tokenFile]");
            //}

            // Validate Token File
            if (!System.IO.File.Exists(tokenFile))
            {
                NonBlockingConsole.WriteError("Token File does not exist!");
                Console.ReadLine();
            }
            else
            {
                var proxy = new cloudsql_proxy_cs.Proxy();

                proxy.OnStatusChanged += (object sender, cloudsql_proxy_cs.StatusEventArgs status) =>
                {
                    NonBlockingConsole.WriteLine($"Status from instance: {status.Instance}: {status.Status}");
                };

                NonBlockingConsole.WriteLine("Type :quit to exit");
                var input = Console.ReadLine().ToLower().Trim();
                while (input != ":quit" || !exit)
                {
                    switch (input.Split(" ".ToCharArray())[0])
                    {
                        case "start":
                            {
                                var instance = "";
                                try
                                {
                                    instance = input.Split(" ".ToCharArray())[1];
                                }
                                catch { }
                                if (!string.IsNullOrWhiteSpace(instance))
                                {
                                    proxy.StartProxy(cloudsql_proxy_cs.AuthenticationMethod.CredentialFile, instance, tokenFile);
                                }
                                else
                                {
                                    Console.WriteLine("Usage: start [instance]");
                                }
                            }
                            break;
                        case "status":
                            {
                                var instance = "";
                                try
                                {
                                    instance = input.Split(" ".ToCharArray())[1];
                                }
                                catch { }
                                if (!string.IsNullOrWhiteSpace(instance))
                                {
                                    NonBlockingConsole.WriteLine($"Status: {proxy.GetStatus(instance)}");
                                }
                                else
                                {
                                    Console.WriteLine("Usage: status [instance]");
                                }
                            }
                            break;
                        case "stop":
                            {
                                var instance = "";
                                try
                                {
                                    instance = input.Split(" ".ToCharArray())[1];
                                }
                                catch { }
                                if (!string.IsNullOrWhiteSpace(instance))
                                {
                                    proxy.StopProxy(instance);
                                }
                                else
                                {
                                    Console.WriteLine("Usage: stop [instance]");
                                }
                            }
                            break;
                        case "help":
                            Console.WriteLine("Usage: start [instance]");
                            Console.WriteLine("       status [instance]");
                            Console.WriteLine("       stop [instance]");
                            break;
                        case ":quit":
                            exit = true;
                            break;
                        default:
                            NonBlockingConsole.WriteLine("Type :quit to exit");
                            break;
                    }
                    input = Console.ReadLine()?.ToLower()?.Trim();
                }

                NonBlockingConsole.WriteDebug("Shutting down thread");
                NonBlockingConsole.WriteDebug("Good bye");
            }
        }
    }
}

