﻿// Copyright 2020 Jin Chan Digital Development. All Rights Reserved.
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

namespace cmd
{
    public static class NonBlockingConsole
    {
        //private static BlockingCollection<KeyValuePair<ConsoleColor, string>> m_Queue = new BlockingCollection<KeyValuePair<ConsoleColor, string>>();

        //static NonBlockingConsole()
        //{
        //    var thread = new Thread(
        //      () =>
        //      {
        //          while (true)
        //          {
        //              var item = m_Queue.Take();
        //              var currentColour = Console.ForegroundColor;
        //              if (item.Key != ConsoleColor.White)
        //              {
        //                  Console.ForegroundColor = item.Key;
        //              }
        //              Console.WriteLine(item.Value);
        //              Console.ForegroundColor = currentColour;
        //          }
        //      });
        //    thread.IsBackground = true;
        //    thread.Start();
        //}

        /// <summary>
        /// Having issues with this library on linux - so this is going to now be a standard console
        /// </summary>
        private static class Queue
        {
            public static void Add(KeyValuePair<ConsoleColor, string> item)
            {
                var currentColour = Console.ForegroundColor;
                if (item.Key != ConsoleColor.White)
                {
                    Console.ForegroundColor = item.Key;
                }
                Console.WriteLine(item.Value);
                Console.ForegroundColor = currentColour;
            }
        }

        public static void WriteLine(string value)
        {
            Queue.Add(new KeyValuePair<ConsoleColor, string>(ConsoleColor.White, value));
        }

        public static void WriteLine(string value, params object[] p)
        {
            Queue.Add(new KeyValuePair<ConsoleColor, string>(ConsoleColor.White, string.Format(value, p)));
        }

        public static void WriteSuccess(string value)
        {
            Queue.Add(new KeyValuePair<ConsoleColor, string>(ConsoleColor.Green, value));
        }

        public static void WriteSuccess(string value, params object[] p)
        {
            Queue.Add(new KeyValuePair<ConsoleColor, string>(ConsoleColor.Green, string.Format(value, p)));
        }

        public static void WriteDebug(string value)
        {
            Queue.Add(new KeyValuePair<ConsoleColor, string>(ConsoleColor.Yellow, value));
        }

        public static void WriteDebug(string value, params object[] p)
        {
            Queue.Add(new KeyValuePair<ConsoleColor, string>(ConsoleColor.Yellow, string.Format(value, p)));
        }

        public static void WriteError(string value)
        {
            Queue.Add(new KeyValuePair<ConsoleColor, string>(ConsoleColor.Red, value));
        }

        public static void WriteError(string value, params object[] p)
        {
            Queue.Add(new KeyValuePair<ConsoleColor, string>(ConsoleColor.Red, string.Format(value, p)));
        }
    }
}
