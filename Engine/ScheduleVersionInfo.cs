#nullable enable

using System;
using System.Linq;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Models;

namespace KdyPojedeVlak.Engine
{
    public static class ScheduleVersionInfo
    {
        private static DateTime lastDownload;
        private static DateTime latestImport;
        private static DateTime newestData;
        private static string newestTrainId;

        private static readonly object syncObj = new object();

        public static void Initialize(DbModelContext dbModelContext)
        {
            lock (syncObj)
            {
                latestImport = dbModelContext.ImportedFiles.Max(f => f.ImportTime);
                newestData = dbModelContext.ImportedFiles.Max(f => f.CreationDate);
            }
        }

        public static void ReportLastDownload(DateTime lastDownloadTimestamp)
        {
            lock (syncObj)
            {
                if (lastDownloadTimestamp > lastDownload) lastDownload = lastDownloadTimestamp;
            }
        }

        public static void ReportDownloadChecked()
        {
            lock (syncObj)
            {
                lastDownload = DateTime.UtcNow;
            }
        }

        public static void ReportFileImported(DateTime dataTimestamp, string trainId)
        {
            lock (syncObj)
            {
                latestImport = DateTime.UtcNow;
                if (dataTimestamp > newestData)
                {
                    newestData = dataTimestamp;
                    newestTrainId = trainId;
                }
            }
        }

        public static VersionInformation CurrentVersionInformation
        {
            get
            {
                lock (syncObj)
                {
                    return new VersionInformation(lastDownload, latestImport, newestData, newestTrainId);
                }
            }
        }
    }
}