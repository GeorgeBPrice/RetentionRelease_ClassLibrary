namespace Retention.Lib.Models.Configuration;

/// <summary>
/// Configuration options for the DevOps Deploy client.
/// </summary>
public record DevOpsDeployOptions
{
    /// <summary>
    /// The base URL of the DevOps Deploy API (e.g., https://your-octopus-url/api)
    /// </summary>
    public string? ApiBaseUrl { get; init; }

    /// <summary>
    /// The API key Name, used as a header name (e.g., X-DevOpsDeploy-ApiKey)
    /// </summary>
    public string? ApiKeyHeader { get; init; }

    /// <summary>
    /// The API key to use for authentication
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// The space ID to use for API requests (e.g., spaces-1)
    /// </summary>
    public string? SpaceId { get; init; } = "spaces-1";

    /// <summary>
    /// The timeout for HTTP requests in seconds. Defaults to 30.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;
} 