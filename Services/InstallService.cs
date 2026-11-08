using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GameLauncher.Config;
using GameLauncher.Models;

namespace GameLauncher.Services
{
    public class InstallProgress
    {
        public string Stage { get; set; } = "";   // "Downloading game", "Extracting", "Downloading data", ...
        public double Percent { get; set; }        // 0-100, -1 for indeterminate
    }

    public static class InstallService
    {
        private static readonly HttpClient _http = new HttpClient();

        public static bool IsGameInstalled() => File.Exists(AppConfig.LocalVersionFilePath);

        public static LocalVersionInfo? GetLocalVersion()
        {
            try
            {
                if (!File.Exists(AppConfig.LocalVersionFilePath)) return null;
                var json = File.ReadAllText(AppConfig.LocalVersionFilePath);
                return JsonSerializer.Deserialize<LocalVersionInfo>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Full install or update flow: download game zip -> extract to install dir,
        /// download data zip -> extract to AppData dir, write version.json.
        /// Safe to call for both a fresh install and an update (overwrites in place).
        /// </summary>
        public static async Task InstallOrUpdateAsync(GameInfo game, IProgress<InstallProgress> progress, CancellationToken ct = default)
        {
            Directory.CreateDirectory(AppConfig.GameInstallDir);
            Directory.CreateDirectory(AppConfig.GameAppDataDir);

            // 1. Game build
            var gameZipPath = Path.Combine(Path.GetTempPath(), $"game_{game.Version}.zip");
            await DownloadFileAsync(game.Url, gameZipPath, "Downloading game", progress, ct);

            progress.Report(new InstallProgress { Stage = "Extracting game files", Percent = -1 });
            ExtractZip(gameZipPath, AppConfig.GameInstallDir);
            SafeDelete(gameZipPath);

            // 2. Extra AppData zip (configs/mods/saves/etc - only if manifest supplies one)
            if (!string.IsNullOrWhiteSpace(game.DataZipUrl))
            {
                var dataZipPath = Path.Combine(Path.GetTempPath(), $"data_{game.Version}.zip");
                await DownloadFileAsync(game.DataZipUrl, dataZipPath, "Downloading game data", progress, ct);

                progress.Report(new InstallProgress { Stage = "Extracting game data", Percent = -1 });
                ExtractZip(dataZipPath, AppConfig.GameAppDataDir);
                SafeDelete(dataZipPath);
            }

            // 3. Record what's now installed
            var versionInfo = new LocalVersionInfo { Version = game.Version, InstalledAt = DateTime.UtcNow };
            File.WriteAllText(AppConfig.LocalVersionFilePath, JsonSerializer.Serialize(versionInfo));

            progress.Report(new InstallProgress { Stage = "Done", Percent = 100 });
        }

        // Extracting on top of an existing folder overwrites matching files, which is
        // what we want for updates (keeps unrelated user files like save data untouched
        // if they live outside the zip's file list).
        private static void ExtractZip(string zipPath, string destinationDir)
        {
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                var destPath = Path.Combine(destinationDir, entry.FullName);
                var destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);

                if (!string.IsNullOrEmpty(entry.Name)) // skip directory entries
                {
                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }
        }

        private static async Task DownloadFileAsync(string url, string destPath, string stageName,
            IProgress<InstallProgress> progress, CancellationToken ct)
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long totalRead = 0;
            int read;
            while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                totalRead += read;

                var percent = totalBytes > 0 ? (double)totalRead / totalBytes * 100 : -1;
                progress.Report(new InstallProgress { Stage = stageName, Percent = percent });
            }
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* not fatal */ }
        }
    }
}
