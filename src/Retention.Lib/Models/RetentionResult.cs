namespace Retention.Lib.Models 
{
    
    /// <summary>
    /// Represents the result of a release retention operation, containing enriched details about 
    /// a release and it's project, environment, and deployment time values.
    /// </summary>
    public record RetentionResult(
        string ReleaseId,
        string ProjectId,
        string ProjectName,
        string EnvironmentId,
        string EnvironmentName,
        string Version,
        DateTime LastDeployedAt);
}

