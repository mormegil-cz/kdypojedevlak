using System;
using System.Collections.Generic;

namespace KdyPojedeVlak.Engine
{
    public class ScheduleVersionInfo
    {
        public string CurrentVersion { get; }
        public string CurrentPath { get; }
        public DateTime LastUpdateDate { get; }

        public List<string> Files { get; }

        public HashSet<string> AlreadyImported { get; }

        public ScheduleVersionInfo(string currentVersion, string currentPath, DateTime lastUpdateDate, List<string> files, HashSet<string> alreadyImported)
        {
            CurrentVersion = currentVersion;
            CurrentPath = currentPath;
            LastUpdateDate = lastUpdateDate;
            Files = files;
            AlreadyImported = alreadyImported;
        }
    }
}
