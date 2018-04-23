using System;

namespace IxyCs
{
    public static class Log
    {
        public static int LogLevel = 3;

        public static void Error(string message, params object[] values)
        {
            if(LogLevel >= 1)
                Console.WriteLine("Error: " + message, values);
        }

        public static void Warning(string message, params object[] values)
        {
            if(LogLevel >= 2)
                Console.WriteLine("Warning: " + message, values);
        }

        public static void Notice(string message, params object[] values)
        {
            if(LogLevel >= 3)
                Console.WriteLine("Notice: " + message, values);
        }

        public static void Message(string message, params object[] values)
        {
            Console.WriteLine(message, values);
        }
    }
}