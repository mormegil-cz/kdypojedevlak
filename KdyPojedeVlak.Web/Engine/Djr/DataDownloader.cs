using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreFtp;

namespace KdyPojedeVlak.Web.Engine.Djr;

public partial class DataDownloader
{
    private const string ClientName = "KdyPojedeVlak/CoreFTP";
    private static readonly Uri serverBaseUri = new(@"ftp://ftp.cisjr.cz/draha/celostatni/szdc/");
    private const int BuffSize = 10240;

    private static readonly Regex reFilename = RegexFilename();
    private static readonly Regex reDirectory = RegexDirectory();
    private static readonly Regex reSubdirectory = RegexSubdirectory();

    [GeneratedRegex(@"^([^.]+)\.(XML\.)?ZIP$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexFilename();

    [GeneratedRegex(@"^2[0-9]{3}$", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexDirectory();

    [GeneratedRegex(@"^2[0-9]{3}-[0-9]{2}$", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexSubdirectory();

    private FtpClient ftp;

    public async Task Connect()
    {
        if (ftp != null) throw new InvalidOperationException("Already connected");

        ftp = new FtpClient(new FtpClientConfiguration { Host = serverBaseUri.GetLeftPart(UriPartial.Authority) });
        await ftp.LoginAsync();
        await ftp.SetClientName(ClientName);
        await ftp.ChangeWorkingDirectoryAsync(serverBaseUri.AbsolutePath);
    }

    public async Task Disconnect()
    {
        if (ftp == null) throw new InvalidOperationException("Not connected");
        await ftp.LogOutAsync();
        ftp = null;
    }

    public async Task<Dictionary<string, long>> GetListOfFilesAvailable()
    {
        async Task AddListOfFilesAvailableInDir(Dictionary<string, long> dictionary, string dir)
        {
            var files = (await ftp.ListFilesAsync())
                .Select(file => new { File = file, Match = reFilename.Match(file.Name) })
                .Where(f => f.Match.Success)
                .OrderByDescending(f => f.Match.Groups[1].Value)
                .Select(f => f.File);

            foreach (var file in files)
            {
                dictionary.Add(dir + "/" + file.Name, file.Size);
            }
        }

        var directories = (await ftp.ListDirectoriesAsync())
            .Select(dir => new { Directory = dir, Match = reDirectory.Match(dir.Name) })
            .Where(f => f.Match.Success)
            .Select(f => f.Directory.Name)
            .ToList();

        var results = new Dictionary<string, long>();
        foreach (var dir in directories)
        {
            await ftp.ChangeWorkingDirectoryAsync(dir);

            var subdirectories = (await ftp.ListDirectoriesAsync())
                .Select(subdir => new { Directory = subdir, Match = reSubdirectory.Match(subdir.Name) })
                .Where(f => f.Match.Success)
                .Select(f => f.Directory.Name)
                .ToList();

            foreach (var subdir in subdirectories)
            {
                await ftp.ChangeWorkingDirectoryAsync(subdir);
                await AddListOfFilesAvailableInDir(results, dir + "/" + subdir);
                await ftp.ChangeWorkingDirectoryAsync("..");
            }

            await AddListOfFilesAvailableInDir(results, dir);

            await ftp.ChangeWorkingDirectoryAsync("..");
        }
        return results;
    }

    public async Task<string> GetLatestVersionAvailable()
    {
        var directories = await ftp.ListDirectoriesAsync();
        var newestDirectory = directories
            .Select(dir => new { Directory = dir, Match = reDirectory.Match(dir.Name) })
            .Where(f => f.Match.Success)
            .OrderByDescending(f => f.Directory.Name)
            .FirstOrDefault()
            ?.Directory?.Name;

        if (newestDirectory == null) return null;

        await ftp.ChangeWorkingDirectoryAsync(newestDirectory);

        var files = await ftp.ListFilesAsync();
        var newest = files
            .Select(file => new { File = file, Match = reFilename.Match(file.Name) })
            .Where(f => f.Match.Success)
            .OrderByDescending(f => f.Match.Groups[1].Value)
            .FirstOrDefault();

        await ftp.ChangeWorkingDirectoryAsync("..");

        return newestDirectory + "/" + newest?.Match.Groups[1].Value;
    }

    public async Task<(string, long)> DownloadZip(string path, string destinationFilename)
    {
        var originalDirectory = ftp.WorkingDirectory;
        var versionSegments = path.Split('/');
        for (var i = 0; i < versionSegments.Length - 1; ++i)
        {
            await ftp.ChangeWorkingDirectoryAsync(versionSegments[i]);
        }
        var fileName = versionSegments[^1];
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var size = 0L;
        await using (var receiveStream = await ftp.OpenFileReadStreamAsync(fileName))
        {
            await using (var storeStream = new FileStream(destinationFilename, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var buffer = new byte[BuffSize];
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
        await ftp.ChangeWorkingDirectoryAsync(originalDirectory);
        var hash = hasher.GetHashAndReset();
        return (Convert.ToHexStringLower(hash), size);
    }
}