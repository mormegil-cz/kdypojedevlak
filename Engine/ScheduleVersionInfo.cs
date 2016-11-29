using System;

namespace KdyPojedeVlak.Engine
{
    public class ScheduleVersionInfo
    {
        public string CurrentVersion { get; }
        public string CurrentPath { get; }
        public DateTime LastUpdateDate { get; }

        public ScheduleVersionInfo(string currentVersion, string currentPath, DateTime lastUpdateDate)
        {
            CurrentVersion = currentVersion;
            CurrentPath = currentPath;
            LastUpdateDate = lastUpdateDate;
        }
    }
}
