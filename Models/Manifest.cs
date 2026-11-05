using System.Text.Json.Serialization;

namespace GameLauncher.Models
{
    /// <summary>
    /// Matches manifest.json hosted in the GitHub repo. Keep this schema and the
    /// hosted JSON in sync.
    /// </summary>
    public class Manifest
    {
        [JsonPropertyName("launcher")]
        public LauncherInfo Launcher { get; set; } = new();

        [JsonPropertyName("game")]
        public GameInfo Game { get; set; } = new();
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

        // Direct download URL to the extra zip that gets unpacked into %AppData%
        [JsonPropertyName("dataZipUrl")]
        public string DataZipUrl { get; set; } = "";

        [JsonPropertyName("changelog")]
        public string Changelog { get; set; } = "";

        [JsonPropertyName("sizeMB")]
        public long SizeMB { get; set; }
    }
}
