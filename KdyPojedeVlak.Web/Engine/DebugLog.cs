using System;
using System.IO;
using System.Text;
using System.Threading;

namespace KdyPojedeVlak.Web.Engine;

// TODO: Migrate to ILogger or some other logging framework
public static class DebugLog
{
    private static readonly bool logDisabled = Environment.GetEnvironmentVariable("KDYPOJEDEVLAK_LOG") == "disabled";
    private static readonly string logFilename = Environment.GetEnvironmentVariable("KDYPOJEDEVLAK_LOGFILE");
    private static readonly Lock initLock = new();
    private static volatile TextWriter logWriter;

    private static TextWriter InitLogWriter()
    {
        if (logDisabled || logWriter != null) return logWriter;

        lock (initLock)
        {
            if (logWriter != null) return logWriter;

            if (logFilename == null)
            {
                logWriter = Console.Out;
                return logWriter;
            }

            try
            {
                logWriter = new StreamWriter(logFilename, false, Encoding.UTF8);
                return logWriter;
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("Error opening log file: " + e);
                logWriter = null;
                return Console.Error;
            }
        }
    }

    private static void WriteLogMessage(string type, string msgFormat, params object[] args)
    {
        if (logDisabled) return;

        var writer = InitLogWriter();

        try
        {
            writer.WriteLine($"{DateTime.UtcNow:u}\t{type}\t{msgFormat}", args);
            writer.Flush();
        }
        catch (IOException e)
        {
            Console.Error.WriteLine("Error writing to log file: " + e);
        }
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