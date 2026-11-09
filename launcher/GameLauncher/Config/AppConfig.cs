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
        public static readonly string CompanyName = "lfairclo";      // used for folder paths
        public static readonly string GameName = "RowHero";          // used for folder paths + window title
        public static readonly string GameExeName = "RowHero.exe";   // the exe Unity produces

        // GitHub repo that hosts manifest.json + releases (zips)
        public static readonly string GitHubOwner = "RowHeroGame";
        public static readonly string GitHubRepo = "RowHero";
        public static readonly string ManifestBranch = "main";

        // Bump this constant every time you ship a new build of the LAUNCHER itself.
        // The launcher compares this to manifest.json's "launcher.version" on startup.
        public static readonly string CurrentLauncherVersion = "1.4.0";
        // --- END EDIT ---


        public static string ManifestUrl =>
            $"https://raw.githubusercontent.com/{GitHubOwner}/{GitHubRepo}/{ManifestBranch}/manifest.json";

        // ---- Game (the Unity build) ----
        // LocalAppData needs no admin rights and is the standard spot for per-user installs.
        public static string GameInstallDir =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                CompanyName, GameName);

        public static string GameExePath => Path.Combine(GameInstallDir, GameExeName);

        public static string LocalVersionFilePath => Path.Combine(GameInstallDir, "version.json");

        // ---- Game data (tracked separately from the game) ----
        // %UserProfile%\AppData\LocalLow - .NET has no SpecialFolder for it, so build it
        // from LocalAppData's parent.
        private static string LocalLowDir =>
            Path.Combine(
                Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))!.FullName,
                "LocalLow");

        // The game-data zip is extracted HERE: ...\LocalLow\<CompanyName>
        // The zip must contain the "<GameName>" folder itself at its top level,
        // so extracting it creates ...\LocalLow\<CompanyName>\<GameName>\...
        public static string GameDataExtractRoot => Path.Combine(LocalLowDir, CompanyName);

        // The folder that results from extracting (used to check the data really is there).
        public static string GameDataDir => Path.Combine(GameDataExtractRoot, GameName);

        // Which data version is installed. Kept next to the game's version.json,
        // NOT inside the data folder, so a data zip can never overwrite it.
        public static string LocalDataVersionFilePath => Path.Combine(GameInstallDir, "data_version.json");

        public static string LauncherInstallDir => AppDomain.CurrentDomain.BaseDirectory;
    }
}
