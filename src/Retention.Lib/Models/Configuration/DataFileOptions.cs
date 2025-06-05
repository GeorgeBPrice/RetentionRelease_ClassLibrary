namespace Retention.Lib.Models.Configuration;

/// <summary>
/// Configuration options for data file locations, used to specify paths for various data files.
/// </summary>
public record DataFileOptions
{
    /// <summary>
    /// Path to the projects data file
    /// </summary>
    public required string Projects { get; init; }

    /// <summary>
    /// Path to the environments data file
    /// </summary>
    public required string Environments { get; init; }

    /// <summary>
    /// Path to the releases data file
    /// </summary>
    public required string Releases { get; init; }

    /// <summary>
    /// Path to the deployments data file
    /// </summary>
    public required string Deployments { get; init; }
} 