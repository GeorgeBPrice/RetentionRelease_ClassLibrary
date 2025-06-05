namespace Retention.Lib.Models
{
    
    ///<summary>
    /// Represents a Deployment record. Records are used here for their concise syntax, immutability, and value-based equality,
    /// which is ideal for modeling simple data structures. This matches the typical JSON data format, where objects are
    /// collections of key-value pairs and are often treated as immutable data transfer objects.
    /// </summary>
    public record Deployment(
        string Id, 
        string ReleaseId, 
        string EnvironmentId, 
        DateTime DeployedAt);
}
