using Microsoft.Extensions.Logging;
using Retention.Lib.Models;

namespace Retention.Lib.Helpers;


/// <summary>
/// Provides helper methods for logging data quality issues such as duplicates and invalid entries
/// in collections of domain models (e.g., Release, Deployment).
/// </summary>
public static class LoggingHelper
{
    public static void LogDuplicates<T>(IEnumerable<T> items, string itemType, Func<T, string?> keySelector, ILogger logger)
    {
        var duplicates = items
            .Where(i => i != null)
            .GroupBy(keySelector)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
            .ToList();

        foreach (var duplicate in duplicates)
        {
            logger.LogWarning(
                "Found {Count} duplicate {ItemType} entries with Id '{Id}'. Using the first occurrence.",
                duplicate.Count(), itemType, duplicate.Key);
        }
    }

    public static void LogInvalidReleases(IEnumerable<Release> releases, Dictionary<string, Project> projectById, ILogger logger)
    {
        var nullReleases = releases.Count(r => r == null);
        if (nullReleases > 0)
        {
            logger.LogWarning("Found {Count} null Release entries.", nullReleases);
        }

        var emptyIdReleases = releases.Count(r => r != null && string.IsNullOrWhiteSpace(r.Id));
        if (emptyIdReleases > 0)
        {
            logger.LogWarning("Found {Count} Release entries with missing or empty Id.", emptyIdReleases);
        }
    }

    public static void LogInvalidDeployments(
        IEnumerable<Deployment> deployments, 
        Dictionary<string, Release> releaseById,
        Dictionary<string, Models.Environment> environmentById,
        ILogger logger)
    {
        var nullDeployments = deployments.Count(d => d == null);
        if (nullDeployments > 0)
        {
            logger.LogWarning("Found {Count} null Deployment entries.", nullDeployments);
        }

        var emptyReleaseIdDeployments = deployments.Count(d => d != null && string.IsNullOrWhiteSpace(d.ReleaseId));
        if (emptyReleaseIdDeployments > 0)
        {
            logger.LogWarning("Found {Count} Deployment entries with missing or empty ReleaseId.", emptyReleaseIdDeployments);
        }

        var emptyEnvironmentIdDeployments = deployments.Count(d => d != null && string.IsNullOrWhiteSpace(d.EnvironmentId));
        if (emptyEnvironmentIdDeployments > 0)
        {
            logger.LogWarning("Found {Count} Deployment entries with missing or empty EnvironmentId.", emptyEnvironmentIdDeployments);
        }
    }
} 