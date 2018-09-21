using System;

namespace KdyPojedeVlak.Engine
{
    // TODO: Migrate to ILogger or some other logging framework
    public static class DebugLog
    {
        private static bool logDisabled = Environment.GetEnvironmentVariable("KDYPOJEDEVLAK_LOG") == "disabled";

        private static void WriteLogMessage(string msgFormat, params object[] args)
        {
            if (logDisabled) return;

            Console.WriteLine(msgFormat, args);
        }

        public static void LogProblem(string msg)
        {
            WriteLogMessage("{0}", msg);
        }

        public static void LogProblem(string msgFormat, params object[] args)
        {
            WriteLogMessage(msgFormat, args);
        }

        public static void LogDebugMsg(string msg)
        {
            WriteLogMessage("{0}", msg);
        }

        public static void LogDebugMsg(string msgFormat, params object[] args)
        {
            WriteLogMessage(msgFormat, args);
        }
    }
}