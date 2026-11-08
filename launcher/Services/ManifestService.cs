using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GameLauncher.Config;
using GameLauncher.Models;

namespace GameLauncher.Services
{
    public static class ManifestService
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        /// <summary>
        /// Downloads and parses manifest.json. Returns null if it can't be reached
        /// (e.g. no internet) - callers should handle that by letting the user
        /// play whatever is already installed, or show an offline message.
        /// </summary>
        public static async Task<Manifest?> FetchAsync()
        {
            try
            {
                // Cache-busting query param - raw.githubusercontent.com is CDN cached for
                // a few minutes, this avoids users seeing stale version info.
                var url = $"{AppConfig.ManifestUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                var json = await _http.GetStringAsync(url);
                return JsonSerializer.Deserialize<Manifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }
    }
}
