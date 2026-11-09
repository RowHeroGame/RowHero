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
        public string Stage { get; set; } = "";   // "Downloading game", "Extracting", ...
        public double Percent { get; set; }        // 0-100, -1 for indeterminate
    }

    public static class InstallService
    {
        private static readonly HttpClient _http = new HttpClient();

        // =====================================================================
        //  GAME  (the Unity build -> %LocalAppData%\Company\Game)
        // =====================================================================

        public static bool IsGameInstalled() => File.Exists(AppConfig.LocalVersionFilePath);

        public static LocalVersionInfo? GetLocalVersion() => ReadVersionFile(AppConfig.LocalVersionFilePath);

        /// <summary>Install or update the game build only. Safe to call for both.</summary>
        public static async Task InstallOrUpdateGameAsync(GameInfo game, IProgress<InstallProgress> progress, CancellationToken ct = default)
        {
            Directory.CreateDirectory(AppConfig.GameInstallDir);

            var zipPath = Path.Combine(Path.GetTempPath(), $"game_{game.Version}.zip");
            await DownloadFileAsync(game.Url, zipPath, "Downloading game", progress, ct);

            progress.Report(new InstallProgress { Stage = "Extracting game files", Percent = -1 });
            await Task.Run(() => ExtractZip(zipPath, AppConfig.GameInstallDir), ct);
            SafeDelete(zipPath);

            WriteVersionFile(AppConfig.LocalVersionFilePath, game.Version);
            progress.Report(new InstallProgress { Stage = "Done", Percent = 100 });
        }

        // =====================================================================
        //  GAME DATA  (separate zip -> %UserProfile%\AppData\LocalLow\Company)
        // =====================================================================

        /// <summary>
        /// Installed data version, or null if data was never installed - or if the
        /// data folder has since been deleted, so it gets offered for install again.
        /// </summary>
        public static LocalVersionInfo? GetLocalDataVersion()
        {
            if (!Directory.Exists(AppConfig.GameDataDir)) return null;
            return ReadVersionFile(AppConfig.LocalDataVersionFilePath);
        }

        /// <summary>
        /// Install or update the game data only. The zip contains the "RowHero" folder
        /// itself, so it is extracted into ...\LocalLow\lfairclo and creates
        /// ...\LocalLow\lfairclo\RowHero.
        /// </summary>
        public static async Task InstallOrUpdateGameDataAsync(GameDataInfo data, IProgress<InstallProgress> progress, CancellationToken ct = default)
        {
            Directory.CreateDirectory(AppConfig.GameDataExtractRoot);

            var zipPath = Path.Combine(Path.GetTempPath(), $"gamedata_{data.Version}.zip");
            await DownloadFileAsync(data.Url, zipPath, "Downloading song data", progress, ct);

            progress.Report(new InstallProgress { Stage = "Extracting song data", Percent = -1 });
            await Task.Run(() => ExtractZip(zipPath, AppConfig.GameDataExtractRoot), ct);
            SafeDelete(zipPath);

            // Catch a wrongly-packed zip (contents zipped instead of the folder itself)
            if (!Directory.Exists(AppConfig.GameDataDir))
            {
                throw new InvalidDataException(
                    $"The song data zip didn't contain a \"{AppConfig.GameName}\" folder at its top level. " +
                    $"Zip the {AppConfig.GameName} folder itself, not the files inside it.");
            }

            WriteVersionFile(AppConfig.LocalDataVersionFilePath, data.Version);
            progress.Report(new InstallProgress { Stage = "Done", Percent = 100 });
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        private static LocalVersionInfo? ReadVersionFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonSerializer.Deserialize<LocalVersionInfo>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        private static void WriteVersionFile(string path, string version)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var info = new LocalVersionInfo { Version = version, InstalledAt = DateTime.UtcNow };
            File.WriteAllText(path, JsonSerializer.Serialize(info));
        }

        // Extracting on top of an existing folder overwrites files that are in the zip
        // and leaves everything else alone.
        private static void ExtractZip(string zipPath, string destinationDir)
        {
            var root = Path.GetFullPath(destinationDir);
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
                root += Path.DirectorySeparatorChar;

            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                var destPath = Path.GetFullPath(Path.Combine(destinationDir, entry.FullName));

                // Never write outside the destination folder, whatever the zip says
                if (!destPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destPath); // folder entry
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                entry.ExtractToFile(destPath, overwrite: true);
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
