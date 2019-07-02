using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KdyPojedeVlak.Engine.Djr;
using Newtonsoft.Json;

namespace KdyPojedeVlak.Engine
{
    public class ScheduleVersionManager
    {
        private const string dataDirectoryPrefix = "data-";
        private const string dataFilePattern = "*.zip";
        private const string metadataNameFormat = "{0}._info";
        private const string updateStatusFileName = ".update_info.json";
        private const int MIN_UPDATE_FREQ_HRS = 8;

        private readonly string basePath;

        public ScheduleVersionManager(string basePath)
        {
            this.basePath = basePath;
        }

        public async Task<Dictionary<string, long>> DownloadMissingFiles()
        {
            var dataFilesAvailable = GetDataFilesAvailable();

            // 2. check online if not too soon after last update
            var lastUpdateDate = GetLastUpdateDate();
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
                        var filePath = Path.Combine(basePath, file.Key.Replace('/', Path.DirectorySeparatorChar));
                        if (dataFilesAvailable.TryGetValue(file.Key, out var currentSize) && currentSize == file.Value) continue;

                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Exists)
                        {
                            DebugLog.LogProblem("Data file {0} size mismatch: {1} expected, {2} found, deleting", file.Key, file.Value, fileInfo.Length);
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
                        dataFilesAvailable[file.Key] = size;
                        DebugLog.LogDebugMsg("Downloaded {0} ({1} B: {2})", file.Key, size, hash);
                    }

                    WriteLastUpdateDate(downloadTime);
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

//            var currentNewestVersionClean = VersionStringToFileName(currentNewestVersion);
//            var pathToDir = dataDirectoryPrefix + currentNewestVersionClean;
//            var dataPath = downloader.ShouldExtractZip ? pathToDir : Path.Combine(pathToDir, $"{currentNewestVersionClean}.zip");
//            return new ScheduleVersionInfo(currentNewestVersion, Path.Combine(basePath, dataPath), lastUpdateDate);
            return dataFilesAvailable;
        }

        private Dictionary<string, long> GetDataFilesAvailable()
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

        private static string VersionStringToFileName(string versionString) => versionString.Replace('/', '_').Replace('\\', '_');

        private DateTime GetLastUpdateDate()
        {
            var updateStatusPath = Path.Combine(basePath, updateStatusFileName);
            if (!File.Exists(updateStatusPath)) return DateTime.MinValue;
            var updateStatusContents = File.ReadAllText(updateStatusPath);
            var data = JsonConvert.DeserializeObject<UpdateStatusData>(updateStatusContents);
            if (data == null) return DateTime.MinValue;
            return data.LastUpdate;
        }

        private void WriteLastUpdateDate(DateTime date)
        {
            var data = new UpdateStatusData {LastUpdate = date};
            var json = JsonConvert.SerializeObject(data);
            var updateStatusPath = Path.Combine(basePath, updateStatusFileName);
            File.WriteAllText(updateStatusPath, json);
        }

        private string GetCurrentNewestVersion()
        {
            var newestDirectory = Directory.EnumerateDirectories(basePath, dataDirectoryPrefix + "*").OrderByDescending(n => n).FirstOrDefault();
            return newestDirectory == null ? null : Path.GetFileName(newestDirectory).Substring(dataDirectoryPrefix.Length);
        }

        private void ExtractZip(string zipName, string destinationDir)
        {
            using (var stream = new FileStream(zipName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true, Encoding.ASCII))
                {
                    zip.ExtractToDirectory(destinationDir);
                }
            }
        }

        private void WriteMetadata(string filename, string version, DateTime downloadTime, string zipHash, long zipSize)
        {
            using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.WriteLine("{{\"version\":\"0\",\"timestamp\":\"{0}\",\"downloaded\":\"{1:o}\",\"zipsize\":\"{2}\",\"ziphash\":\"{3}\"}}", version, downloadTime, zipSize, zipHash);
                }
            }
        }

        private class UpdateStatusData
        {
            public DateTime LastUpdate { get; set; }
        }
    }
}