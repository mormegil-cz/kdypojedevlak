#nullable enable

using System;
using System.Linq;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Models;
using Microsoft.EntityFrameworkCore;

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
                latestImport = dbModelContext.ImportedFiles.Max(f => (DateTime?) f.ImportTime) ?? DateTime.MinValue;
                var newestFile = dbModelContext.ImportedFiles.OrderByDescending(f => f.CreationDate).FirstOrDefault();
                if (newestFile == null)
                {
                    newestData = DateTime.MinValue;
                    newestTrainId = null;
                }
                else
                {
                    newestData = newestFile.CreationDate;
                    var newestTimetable = dbModelContext.TrainTimetables.Include(tt => tt.Train).FirstOrDefault(tt => tt.Variants.Any(ttv => ttv.ImportedFrom == newestFile));
                    newestTrainId = newestTimetable?.TrainNumber;
                }
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