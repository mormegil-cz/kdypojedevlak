using System;
using System.IO;
using System.Text;

namespace KdyPojedeVlak.Web.Engine
{
    // TODO: Migrate to ILogger or some other logging framework
    public static class DebugLog
    {
        private static bool logDisabled = Environment.GetEnvironmentVariable("KDYPOJEDEVLAK_LOG") == "disabled";
        private static string logFilename = Environment.GetEnvironmentVariable("KDYPOJEDEVLAK_LOGFILE");
        private static TextWriter logWriter = logFilename == null ? Console.Out : new StreamWriter(logFilename, false, Encoding.UTF8);

        private static void WriteLogMessage(string type, string msgFormat, params object[] args)
        {
            if (logDisabled) return;

            logWriter.WriteLine(type + "\t" + msgFormat, args);
            logWriter.Flush();
        }

        public static void LogProblem(string msg)
        {
            WriteLogMessage("WARN", "{0}", msg);
        }

        public static void LogProblem(string msgFormat, params object[] args)
        {
            WriteLogMessage("WARN", msgFormat, args);
        }

        public static void LogDebugMsg(string msg)
        {
            WriteLogMessage("DEBUG", "{0}", msg);
        }

        public static void LogDebugMsg(string msgFormat, params object[] args)
        {
            WriteLogMessage("DEBUG", msgFormat, args);
        }
    }
}