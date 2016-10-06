using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdyPojedeVlak.Engine
{
    public class ScheduleVersionManager
    {
        private const string dataDirectoryPrefix = "data-";
        private const string metadataNameFormat = "{0}._info";
        private readonly string basePath;

        public ScheduleVersionManager(string basePath)
        {
            this.basePath = basePath;
        }

        public async Task<string> TryUpdate()
        {
            // 1. find current newest version
            var currentNewestVersion = GetCurrentNewestVersion();

            // 2. check online
            var downloader = new DataDownloader();
            await downloader.Connect();
            try
            {
                var onlineNewestVersion = await downloader.GetLatestVersionAvailable();

                if (currentNewestVersion == null || String.CompareOrdinal(onlineNewestVersion, currentNewestVersion) > 0)
                {
                    // 3. if newer available, download and extract
                    var tempName = $"temp-{onlineNewestVersion}";
                    var finalDirName = dataDirectoryPrefix + onlineNewestVersion;
                    var zipName = $"temp-{onlineNewestVersion}.zip";
                    var tempDir = Path.Combine(basePath, tempName);
                    var zipPath = Path.Combine(tempDir, zipName);
                    var finalDir = Path.Combine(basePath, finalDirName);
                    Directory.CreateDirectory(tempDir);
                    var downloadTime = DateTime.UtcNow;
                    var downloadInfo = await downloader.DownloadZip(onlineNewestVersion, zipPath);

                    ExtractZip(zipPath, tempDir);
                    File.Delete(zipPath);

                    WriteMetadata(Path.Combine(tempDir, String.Format(CultureInfo.InvariantCulture, metadataNameFormat, onlineNewestVersion)), onlineNewestVersion, downloadTime, downloadInfo.Item1, downloadInfo.Item2);

                    Directory.Move(tempDir, finalDir);

                    currentNewestVersion = onlineNewestVersion;
                }
            }
            finally
            {
                try
                {
                    await downloader.Disconnect();
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Unexpected exception in DataDownloader.Disconnect: {0}", ex);
                }
            }

            return Path.Combine(basePath, dataDirectoryPrefix + currentNewestVersion);
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
    }
}
