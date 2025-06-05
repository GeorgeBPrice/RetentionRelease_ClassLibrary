using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Retention.Lib.Interfaces;
using Retention.Lib.Models;
using Retention.Lib.Models.Configuration;

namespace Retention.Lib.Clients;

/// <summary>
/// API Client for interacting with the DevOps Deploy API.
/// <para>
/// This client mimics the Octopus API client and provides methods to retrieve
/// projects, environments, releases, and deployments from the DevOps Deploy API.
/// </para>
/// </summary>
/// <remarks>
/// The client uses <see cref="HttpClient"/> for HTTP communication and expects
/// configuration via <see cref="DevOpsDeployOptions"/>. Logging is provided via
/// <see cref="ILogger{DevOpsDeployClient}"/>.
/// </remarks>

public class DevOpsDeployClient : IDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DevOpsDeployClient> _logger;
    private readonly DevOpsDeployOptions _options;

    public DevOpsDeployClient(
        HttpClient httpClient,
        DevOpsDeployOptions options,
        ILogger<DevOpsDeployClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
    }

    public async Task<IEnumerable<Project>> GetProjectsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching projects from DevOps Deploy API");
            var response = await _httpClient.GetFromJsonAsync<List<Project>>($"api/{_options.SpaceId}/projects");
            return response ?? Enumerable.Empty<Project>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch projects from DevOps Deploy API");
            throw;
        }
    }

    public async Task<IEnumerable<Models.Environment>> GetEnvironmentsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching environments from DevOps Deploy API");
            var response = await _httpClient.GetFromJsonAsync<List<Models.Environment>>($"api/{_options.SpaceId}/environments");
            return response ?? Enumerable.Empty<Models.Environment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch environments from DevOps Deploy API");
            throw;
        }
    }

    public async Task<IEnumerable<Release>> GetReleasesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching releases from DevOps Deploy API");
            var response = await _httpClient.GetFromJsonAsync<List<Release>>($"api/{_options.SpaceId}/releases");
            return response ?? Enumerable.Empty<Release>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch releases from DevOps Deploy API");
            throw;
        }
    }

    public async Task<IEnumerable<Deployment>> GetDeploymentsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching deployments from DevOps Deploy API");
            var response = await _httpClient.GetFromJsonAsync<List<Deployment>>($"api/{_options.SpaceId}/deployments");
            return response ?? Enumerable.Empty<Deployment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch deployments from DevOps Deploy API");
            throw;
        }
    }
} 