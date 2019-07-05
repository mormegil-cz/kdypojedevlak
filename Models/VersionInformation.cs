#nullable enable

using System;

namespace KdyPojedeVlak.Models
{
    public class VersionInformation
    {
        public readonly DateTime LastDownload;
        public readonly DateTime LatestImport;
        public readonly DateTime NewestData;

        public VersionInformation(DateTime lastDownload, DateTime latestImport, DateTime newestData)
        {
            LastDownload = lastDownload;
            LatestImport = latestImport;
            NewestData = newestData;
        }
    }
}