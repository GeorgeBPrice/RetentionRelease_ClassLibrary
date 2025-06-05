namespace Retention.Lib.Models
{
    
    /// <summary>
    /// Represents the result of a validation process, containing mappings of projects, environments, releases,
    /// and valid deployments by their identifiers. Used to pass validated entities between validation logic and consumers.
    /// </summary>
    public record ValidationResult(
        Dictionary<string, Project> ProjectById,
        Dictionary<string, Environment> EnvironmentById,
        Dictionary<string, Release> ReleaseById,
        Dictionary<string, Deployment> ValidDeployments);
}
