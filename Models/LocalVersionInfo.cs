using System;
using System.Text.Json.Serialization;

namespace GameLauncher.Models
{
    /// <summary>
    /// Written to version.json inside the game install folder after every
    /// successful install/update. Read back on launcher startup to know
    /// what's currently installed.
    /// </summary>
    public class LocalVersionInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "0.0.0";

        [JsonPropertyName("installedAt")]
        public DateTime InstalledAt { get; set; }
    }
}
