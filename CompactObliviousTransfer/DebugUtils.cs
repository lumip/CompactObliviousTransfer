using System;
using System.Threading;

namespace CompactOT
{
    public static class DebugUtils
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
