using Microsoft.Extensions.Logging;
using Retention.Lib.Models;

namespace Retention.Lib.Helpers;


/// <summary>
/// Provides helper methods for validating Release and Deployment objects,
/// logging warnings for invalid data, and ensuring data integrity before processing.
/// </summary>
public static class ValidationHelper
{
    public static bool IsValidRelease(Release release, Dictionary<string, Project> projectById, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(release.ProjectId))
        {
            logger.LogWarning(
                "Skipping Release {ReleaseId} due to missing or empty ProjectId.",
                release.Id);
            return false;
        }

        if (!projectById.ContainsKey(release.ProjectId))
        {
            logger.LogWarning(
                "Skipping Release {ReleaseId} because its ProjectId '{ProjectId}' does not exist.",
                release.Id, release.ProjectId);
            return false;
        }

        if (!VersionValidatorHelper.IsValidVersion(release.Version))
        {
            logger.LogWarning(
                "Skipping Release {ReleaseId} due to invalid version format: '{Version}'",
                release.Id, release.Version ?? "null");
            return false;
        }

        if (release.CreatedAt == DateTime.MaxValue)
        {
            logger.LogWarning(
                "Release {ReleaseId} has an invalid Created date (MaxValue): {Created}",
                release.Id, release.CreatedAt);
        }

        return true;
    }

    public static bool IsValidDeployment(
        Deployment deployment,
        Dictionary<string, Release> releaseById,
        Dictionary<string, Models.Environment> environmentById,
        ILogger logger)
    {
        var isValid = true;

        if (string.IsNullOrWhiteSpace(deployment.ReleaseId))
        {
            logger.LogWarning(
                "Skipping Deployment (ReleaseId: null, EnvironmentId: {EnvironmentId}, DeployedAt: {DeployedAt}) due to missing or empty ReleaseId.",
                deployment.EnvironmentId, deployment.DeployedAt);
            isValid = false;
        }
        else if (!releaseById.ContainsKey(deployment.ReleaseId))
        {
            logger.LogWarning(
                "Skipping Deployment (ReleaseId: {ReleaseId}, EnvironmentId: {EnvironmentId}, DeployedAt: {DeployedAt}) because its ReleaseId does not exist.",
                deployment.ReleaseId, deployment.EnvironmentId, deployment.DeployedAt);
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(deployment.EnvironmentId))
        {
            logger.LogWarning(
                "Skipping Deployment (ReleaseId: {ReleaseId}, EnvironmentId: null, DeployedAt: {DeployedAt}) due to missing or empty EnvironmentId.",
                deployment.ReleaseId, deployment.DeployedAt);
            isValid = false;
        }
        else if (!environmentById.ContainsKey(deployment.EnvironmentId))
        {
            logger.LogWarning(
                "Skipping Deployment (ReleaseId: {ReleaseId}, EnvironmentId: {EnvironmentId}, DeployedAt: {DeployedAt}) because its EnvironmentId does not exist.",
                deployment.ReleaseId, deployment.EnvironmentId, deployment.DeployedAt);
            isValid = false;
        }

        if (deployment.DeployedAt == DateTime.MaxValue)
        {
            logger.LogWarning(
                "Deployment (ReleaseId: {ReleaseId}, EnvironmentId: {EnvironmentId}) has an invalid DeployedAt date (MaxValue): {DeployedAt}",
                deployment.ReleaseId, deployment.EnvironmentId, deployment.DeployedAt);
        }

        return isValid;
    }
}
