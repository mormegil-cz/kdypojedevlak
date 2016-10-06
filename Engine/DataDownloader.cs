using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreFtp;

namespace KdyPojedeVlak.Engine
{
    public class DataDownloader
    {
        private const string clientName = "KdyPojedeVlak/CoreFTP";
        private static readonly Uri serverBaseUri = new Uri(@"ftp://ftp.cisjr.cz/draha/celostatni/");
        private const string filenameFormat = "VS_{0}.ZIP";
        private static readonly Regex reFilename = new Regex(@"^VS_([0-9_-]+)\.ZIP$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private const int BUFF_SIZE = 10240;

        private FtpClient ftp;

        public async Task Connect()
        {
            if (ftp != null) throw new InvalidOperationException("Already connected");

            ftp = new FtpClient(new FtpClientConfiguration { Host = serverBaseUri.Host });
            await ftp.LoginAsync();
            await ftp.SetClientName(clientName);
            await ftp.ChangeWorkingDirectoryAsync(serverBaseUri.AbsolutePath);
        }

        public async Task Disconnect()
        {
            if (ftp == null) throw new InvalidOperationException("Not connected");
            await ftp.LogOutAsync();
            ftp = null;
        }

        public async Task<string> GetLatestVersionAvailable()
        {
            var files  = await ftp.ListFilesAsync();
            var newest = files
                .Select(file => new { File = file, Match = reFilename.Match(file.Name) })
                .Where(f => f.Match.Success)
                .OrderByDescending(f => f.Match.Groups[1].Value)
                .FirstOrDefault();
            if (newest == null) return null;
            return newest.Match.Groups[1].Value;
        }

        public async Task<Tuple<string, long>> DownloadZip(string version, string destinationFilename)
        {
            using (var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var size = 0L;
                using (var receiveStream = await ftp.OpenFileReadStreamAsync(String.Format(CultureInfo.InvariantCulture, filenameFormat, version)))
                {
                    using (var storeStream = new FileStream(destinationFilename, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        var buffer = new byte[BUFF_SIZE];
                        while (true)
                        {
                            var read = await receiveStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0) break;
                            var writeTask = storeStream.WriteAsync(buffer, 0, read);
                            hasher.AppendData(buffer, 0, read);
                            size += read;
                            await writeTask;
                        }
                    }
                }
                var hash = hasher.GetHashAndReset();
                return Tuple.Create(BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant(), size);
            }
        }
    }
}
