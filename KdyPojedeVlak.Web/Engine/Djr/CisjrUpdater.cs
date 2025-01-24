#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KdyPojedeVlak.Web.Engine.Djr;

public static class CisjrUpdater
{
    private static readonly Lock lastUpdateFileAccess = new();

    private const string DataFilePattern = "*.zip";
    private const string UpdateStatusFileName = ".update_info.json";
    private const int MinUpdateFreqHrs = 1;

    public static async Task<Dictionary<string, long>> DownloadMissingFiles(string basePath)
    {
        var dataFilesAvailable = GetDataFilesAvailable(basePath);

        // check online if not too soon after last update
        var lastUpdateDate = GetLastUpdateDate(basePath);
        ScheduleVersionInfo.ReportLastDownload(lastUpdateDate);
        if (lastUpdateDate <= DateTime.UtcNow.AddHours(-MinUpdateFreqHrs))
        {
            await DownloadNewFiles(basePath, dataFilesAvailable);
        }

        return dataFilesAvailable;
    }

    private static async Task DownloadNewFiles(string basePath, Dictionary<string, long> dataFilesAvailable)
    {
        var downloader = new DataDownloader();

        await downloader.Connect();
        try
        {
            var downloadTime = DateTime.UtcNow;
            var availableFilesForDownload = (await downloader.GetListOfFilesAvailable())
                .Select(file => (
                    Key: file.Key,
                    Size: file.Value,
                    Path: Path.Combine(basePath, file.Key.Replace('/', Path.DirectorySeparatorChar))
                ))
                .Where(file => !dataFilesAvailable.TryGetValue(file.Path, out var currentSize) || currentSize != file.Size)
                .ToList();

            var total = availableFilesForDownload.Count;
            var count = 0;
            foreach (var file in availableFilesForDownload)
            {
                ++count;
                if (dataFilesAvailable.ContainsKey(file.Path))
                {
                    // ?!?
                    DebugLog.LogProblem("Data file {0} appeared unexpectedly!", file.Path);
                    continue;
                }
                var fileInfo = new FileInfo(file.Path);
                if (fileInfo.Exists)
                {
                    DebugLog.LogProblem("Data file {0} size mismatch: {1} expected, {2} found, deleting", file.Key, file.Size, fileInfo.Length);
                    fileInfo.Delete();
                }

                DebugLog.LogDebugMsg("#{0}/{1}: Downloading {2}", count, total, file.Key);
                var dirName = Path.GetDirectoryName(file.Path);
                if (dirName != null && !Directory.Exists(dirName))
                {
                    DebugLog.LogDebugMsg("Creating directory {0}", dirName);
                    Directory.CreateDirectory(dirName);
                }
                var tempFile = Path.ChangeExtension(fileInfo.FullName, ".tmp");
                var (hash, size) = await downloader.DownloadZip(file.Key, tempFile);
                File.Move(tempFile, fileInfo.FullName);
                dataFilesAvailable[file.Path] = size;
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

    private static Dictionary<string, long> GetDataFilesAvailable(string basePath)
    {
        var result = new Dictionary<string, long>();
        foreach (var directory in Directory.GetDirectories(basePath))
        {
            foreach (var subdirectory in Directory.GetDirectories(directory))
            {
                foreach (var file in Directory.GetFiles(subdirectory, DataFilePattern))
                {
                    result.Add(file, new FileInfo(file).Length);
                }
            }
            foreach (var file in Directory.GetFiles(directory, DataFilePattern))
            {
                result.Add(file, new FileInfo(file).Length);
            }
        }
        return result;
    }

    private static DateTime GetLastUpdateDate(string basePath)
    {
        lock (lastUpdateFileAccess)
        {
            var updateStatusPath = Path.Combine(basePath, UpdateStatusFileName);
            if (!File.Exists(updateStatusPath)) return DateTime.MinValue;
            var updateStatusContents = File.ReadAllText(updateStatusPath);
            var data = JsonConvert.DeserializeObject<UpdateStatusData>(updateStatusContents);
            return data?.LastUpdate ?? DateTime.MinValue;
        }
    }

    private static void WriteLastUpdateDate(string basePath, DateTime date)
    {
        lock (lastUpdateFileAccess)
        {
            var data = new UpdateStatusData { LastUpdate = date };
            var json = JsonConvert.SerializeObject(data);
            var updateStatusPath = Path.Combine(basePath, UpdateStatusFileName);
            File.WriteAllText(updateStatusPath, json);
        }
    }

    private class UpdateStatusData
    {
        public DateTime LastUpdate { get; init; }
    }
}