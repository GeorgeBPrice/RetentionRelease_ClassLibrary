namespace Retention.Lib.Models.Configuration;

/// <summary>
/// Configuration options for Retention.Lib
/// </summary>
public record RetentionOptions
{
    /// <summary>
    /// Whether to use local data files instead of DevOps API
    /// </summary>
    public required bool UseLocalData { get; init; }

    /// <summary>
    /// Default number of releases to keep per environment
    /// </summary>
    public required int KeepCount { get; init; }

    /// <summary>
    /// Configuration for local data files. Required when UseLocalData is true.
    /// </summary>
    public DataFileOptions? DataFiles { get; init; }

    /// <summary>
    /// Configuration for DevOps API. Required when UseLocalData is false.
    /// </summary>
    public DevOpsDeployOptions? DevOpsDeploy { get; init; }

    /// <summary>
    /// Validates the configuration based on UseLocalData setting
    /// </summary>
    public void Validate()
    {
        if (UseLocalData && DataFiles == null)
        {
            throw new InvalidOperationException("DataFiles configuration is required when UseLocalData is true");
        }

        if (!UseLocalData && DevOpsDeploy == null)
        {
            throw new InvalidOperationException("DevOpsDeploy configuration is required when UseLocalData is false");
        }

        if (!UseLocalData && string.IsNullOrEmpty(DevOpsDeploy?.ApiBaseUrl))
        {
            throw new InvalidOperationException("DevOpsDeploy.ApiBaseUrl is required when UseLocalData is false");
        }
    }
} 