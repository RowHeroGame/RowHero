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

        // Pad to "major.minor.build" so "1.3" and "1.3.0" count as the same version.
        // (System.Version treats a missing part as -1, so 1.3.0 would beat 1.3 and the
        // launcher would keep "updating" itself to the version it already is.)
        private static string NormalizeForParsing(string v)
        {
            v = v.Trim().TrimStart('v', 'V');
            var parts = v.Split('.').Length;
            for (var i = parts; i < 3; i++) v += ".0";
            return v;
        }
    }
}
