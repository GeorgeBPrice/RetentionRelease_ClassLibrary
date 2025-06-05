

namespace Retention.Lib.Models
{
    /// <summary>
    /// Represents information about a release deployment, including the release identifier,
    /// the release details, the last deployment date, and the total number of deployments.
    /// </summary>
    public record ReleaseDeploymentInfo(
        string ReleaseId,
        Release Release,
        DateTime LastDeployed,
        int DeploymentCount);
}
