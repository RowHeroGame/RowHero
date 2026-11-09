using System.Text.Json.Serialization;

namespace GameLauncher.Models
{
    /// <summary>
    /// Matches manifest.json hosted in the GitHub repo. Keep this schema and the
    /// hosted JSON in sync. The launcher, the game and the game data each have
    /// their own version, url and changelog, so each updates independently.
    /// </summary>
    public class Manifest
    {
        [JsonPropertyName("launcher")]
        public LauncherInfo Launcher { get; set; } = new();

        [JsonPropertyName("game")]
        public GameInfo Game { get; set; } = new();

        // Optional - if missing (or url empty) the launcher hides the game-data row.
        [JsonPropertyName("gameData")]
        public GameDataInfo GameData { get; set; } = new();
    }

    public class LauncherInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "0.0.0";

        // Direct download URL to the launcher's own zip (GitHub Release asset URL)
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
    }

    public class GameInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "0.0.0";

        // Direct download URL to the game build zip (GitHub Release asset URL)
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("changelog")]
        public string Changelog { get; set; } = "";

        [JsonPropertyName("sizeMB")]
        public long SizeMB { get; set; }
    }

    public class GameDataInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "0.0.0";

        // Direct download URL to the data zip. The zip contains the "RowHero" folder
        // itself, and is extracted into %UserProfile%\AppData\LocalLow\<CompanyName>
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("changelog")]
        public string Changelog { get; set; } = "";

        [JsonPropertyName("sizeMB")]
        public long SizeMB { get; set; }
    }
}
