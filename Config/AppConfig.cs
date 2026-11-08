using System;
using System.IO;

namespace GameLauncher.Config
{
    /// <summary>
    /// Every value you need to change to re-brand / re-point this launcher lives here.
    /// Nothing else in the codebase should hardcode a company/game/repo name.
    /// </summary>
    public static class AppConfig
    {
        // --- EDIT THESE ---
        public static readonly string CompanyName = "lfairclo";   // used for folder paths
        public static readonly string GameName = "RowHero";         // used for folder paths + window title
        public static readonly string GameExeName = "RowHero.exe";  // the exe Unity produces

        // GitHub repo that hosts manifest.json + releases (zips)
        public static readonly string GitHubOwner = "lfairclo";
        public static readonly string GitHubRepo = "theo-game-launcher";
        public static readonly string ManifestBranch = "main";

        // Bump this constant every time you ship a new build of the LAUNCHER itself.
        // The launcher compares this to manifest.json's "launcher.version" on startup.
        public static readonly string CurrentLauncherVersion = "1.0.0";
        // --- END EDIT ---


        public static string ManifestUrl =>
            $"https://raw.githubusercontent.com/{GitHubOwner}/{GitHubRepo}/{ManifestBranch}/manifest.json";

        // Main game install location - LocalAppData needs no admin rights and is the
        // standard spot for per-user installed apps that aren't distributed via MSI.
        public static string GameInstallDir =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                CompanyName, GameName);

        // The separate zip you need unpacked into Roaming AppData (configs/mods/saves/etc,
        // kept apart from the install dir so it can survive a reinstall or update).
        public static string GameAppDataDir =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                CompanyName, GameName);

        public static string GameExePath => Path.Combine(GameInstallDir, GameExeName);

        public static string LocalVersionFilePath => Path.Combine(GameInstallDir, "version.json");

        public static string LauncherInstallDir => AppDomain.CurrentDomain.BaseDirectory;
    }
}
