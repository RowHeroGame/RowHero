using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using GameLauncher.Config;

namespace GameLauncher.Services
{
    /// <summary>
    /// A running .exe can't overwrite itself on Windows, so self-updating works like this:
    ///   1. Download the new launcher build to a temp folder and unzip it there.
    ///   2. Write out a small .bat script that: waits for this process to exit,
    ///      copies the new files over the current install dir, then relaunches.
    ///   3. Start that script (hidden) and exit the current process.
    /// This avoids needing a separate compiled "Updater.exe" helper project.
    /// </summary>
    public static class UpdaterService
    {
        private static readonly HttpClient _http = new HttpClient();

        public static async Task<bool> DownloadAndApplyAsync(string downloadUrl)
        {
            var workDir = Path.Combine(Path.GetTempPath(), "GameLauncherUpdate_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workDir);
            var zipPath = Path.Combine(workDir, "launcher_update.zip");
            var extractDir = Path.Combine(workDir, "extracted");

            try
            {
                var bytes = await _http.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(zipPath, bytes);

                ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

                var currentExePath = Process.GetCurrentProcess().MainModule!.FileName!;
                var installDir = AppConfig.LauncherInstallDir;
                var currentPid = Environment.ProcessId;

                var batPath = Path.Combine(workDir, "apply_update.bat");
                var batContents = $@"
@echo off
:waitloop
tasklist /FI ""PID eq {currentPid}"" 2>NUL | find ""{currentPid}"" >NUL
if not errorlevel 1 (
    timeout /t 1 /nobreak >NUL
    goto waitloop
)
xcopy ""{extractDir}\*"" ""{installDir}"" /E /Y /I >NUL
start """" ""{currentExePath}""
rmdir /S /Q ""{workDir}""
del ""%~f0""
";
                await File.WriteAllTextAsync(batPath, batContents);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batPath}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);

                return true;
            }
            catch
            {
                // If self-update fails for any reason, don't block the user from
                // playing with what they've already got - just skip it silently
                // and try again next launch.
                return false;
            }
        }
    }
}
