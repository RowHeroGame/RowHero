using System;

namespace GameLauncher.Services
{
    public static class VersionComparer
    {
        /// <summary>
        /// Returns true if remoteVersion is newer than localVersion.
        /// Expects "1.2.3" style strings. Falls back safely if parsing fails.
        /// </summary>
        public static bool IsNewer(string? localVersion, string? remoteVersion)
        {
            if (string.IsNullOrWhiteSpace(remoteVersion)) return false;
            if (string.IsNullOrWhiteSpace(localVersion)) return true;

            if (Version.TryParse(NormalizeForParsing(localVersion), out var local) &&
                Version.TryParse(NormalizeForParsing(remoteVersion), out var remote))
            {
                return remote > local;
            }

            // Fallback: plain string inequality means "something changed, treat as update"
            return !string.Equals(localVersion.Trim(), remoteVersion.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        // System.Version needs at least "major.minor" - pad "1" -> "1.0"
        private static string NormalizeForParsing(string v)
        {
            v = v.Trim().TrimStart('v', 'V');
            var parts = v.Split('.');
            return parts.Length == 1 ? v + ".0" : v;
        }
    }
}
