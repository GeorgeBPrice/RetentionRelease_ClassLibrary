using System.Text.RegularExpressions;

namespace Retention.Lib.Helpers
{
    /// <summary>
    /// Provides validation of version strings against the Semantic Versioning 2.0.0 specification.
    /// <para>- MAJOR.MINOR.PATCH</para>
    /// <para>- Optional pre-release (e.g., -alpha.1)</para>
    /// <para>- Optional build metadata (e.g., +20130313144700)</para>
    /// </summary>
    public static class VersionValidatorHelper
    {
        private static readonly Regex _semverPattern = new Regex(
          @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)" +
          @"(?:-([0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*))?" +
          @"(?:\+([0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*))?$",
          RegexOptions.Compiled);

        public static bool IsValidVersion(string? version)
        {
            if (string.IsNullOrEmpty(version))
                return false;

            return _semverPattern.IsMatch(version);
        }
    }
}