#nullable enable

using System;

namespace KdyPojedeVlak.Models
{
    public class VersionInformation
    {
        public readonly DateTime LastDownload;
        public readonly DateTime LatestImport;
        public readonly DateTime NewestData;
        public readonly string NewestTrainId;

        public VersionInformation(DateTime lastDownload, DateTime latestImport, DateTime newestData, string newestTrainId)
        {
            LastDownload = lastDownload;
            LatestImport = latestImport;
            NewestData = newestData;
            NewestTrainId = newestTrainId;
        }
    }
}