#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KdyPojedeVlak.Engine.Djr
{
    public static class CisjrUpdater
    {
        private const string dataFilePattern = "*.zip";
        private const string updateStatusFileName = ".update_info.json";
        private const int MIN_UPDATE_FREQ_HRS = 1;

        public static async Task<Dictionary<string, long>> DownloadMissingFiles(string basePath)
        {
            var dataFilesAvailable = GetDataFilesAvailable(basePath);

            // check online if not too soon after last update
            var lastUpdateDate = GetLastUpdateDate(basePath);
            ScheduleVersionInfo.ReportLastDownload(lastUpdateDate);
            if (lastUpdateDate <= DateTime.UtcNow.AddHours(-MIN_UPDATE_FREQ_HRS))
            {
                var downloader = new DataDownloader();

                await downloader.Connect();
                try
                {
                    var downloadTime = DateTime.UtcNow;
                    var availableFilesForDownload = await downloader.GetListOfFilesAvailable();

                    foreach (var file in availableFilesForDownload)
                    {
                        var fileName = file.Key.Replace('/', Path.DirectorySeparatorChar);
                        var filePath = Path.Combine(basePath, fileName);
                        if (dataFilesAvailable.TryGetValue(filePath, out var currentSize) && currentSize == file.Value) continue;

                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Exists)
                        {
                            DebugLog.LogProblem("Data file {0} size mismatch: {1} expected, {2}/{3} found, deleting", file.Key, file.Value, fileInfo.Length, currentSize);
                            fileInfo.Delete();
                        }

                        DebugLog.LogDebugMsg("Downloading {0}", file.Key);
                        var dirName = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(dirName))
                        {
                            DebugLog.LogDebugMsg("Creating directory {0}", dirName);
                            Directory.CreateDirectory(dirName);
                        }
                        var tempFile = Path.ChangeExtension(fileInfo.FullName, ".tmp");
                        var (hash, size) = await downloader.DownloadZip(file.Key, tempFile);
                        File.Move(tempFile, fileInfo.FullName);
                        dataFilesAvailable[fileInfo.FullName] = size;
                        DebugLog.LogDebugMsg("Downloaded {0} ({1} B: {2})", file.Key, size, hash);
                    }

                    WriteLastUpdateDate(basePath, downloadTime);
                    ScheduleVersionInfo.ReportDownloadChecked();
                }
                finally
                {
                    try
                    {
                        await downloader.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        DebugLog.LogProblem("Unexpected exception in DataDownloader.Disconnect: {0}", ex);
                    }
                }
            }

            return dataFilesAvailable;
        }

        private static Dictionary<string, long> GetDataFilesAvailable(string basePath)
        {
            var result = new Dictionary<string, long>();
            foreach (var subdirectory in Directory.GetDirectories(basePath))
            {
                foreach (var file in Directory.GetFiles(subdirectory, dataFilePattern))
                {
                    result.Add(file, new FileInfo(file).Length);
                }
            }
            return result;
        }

        private static DateTime GetLastUpdateDate(string basePath)
        {
            var updateStatusPath = Path.Combine(basePath, updateStatusFileName);
            if (!File.Exists(updateStatusPath)) return DateTime.MinValue;
            var updateStatusContents = File.ReadAllText(updateStatusPath);
            var data = JsonConvert.DeserializeObject<UpdateStatusData>(updateStatusContents);
            if (data == null) return DateTime.MinValue;
            return data.LastUpdate;
        }

        private static void WriteLastUpdateDate(string basePath, DateTime date)
        {
            var data = new UpdateStatusData { LastUpdate = date };
            var json = JsonConvert.SerializeObject(data);
            var updateStatusPath = Path.Combine(basePath, updateStatusFileName);
            File.WriteAllText(updateStatusPath, json);
        }

        private class UpdateStatusData
        {
            public DateTime LastUpdate { get; set; }
        }
    }
}