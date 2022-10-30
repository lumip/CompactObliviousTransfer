// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Threading;

namespace CompactOT
{
    internal static class DebugUtils
    {
        public static void WriteLine(string role, string protocol, string message, params object[] formatArgs)
        {
            Console.WriteLine($"[{role}|{protocol}|{Thread.CurrentThread.ManagedThreadId}] {string.Format(message, formatArgs)}");
        }

        public static void WriteLineReceiver(string protocol, string message, params object[] formatArgs)
        {
            WriteLine("Receiver", protocol, message, formatArgs);
        }

        public static void WriteLineSender(string protocol, string message, params object[] formatArgs)
        {
            WriteLine("Sender", protocol, message, formatArgs);
        }

    }
}
