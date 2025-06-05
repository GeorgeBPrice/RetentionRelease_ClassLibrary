using Microsoft.Extensions.Logging;
using Retention.Lib.Helpers;
using Retention.Lib.Interfaces;
using Retention.Lib.Models;

namespace Retention.Lib.Services;


/// <summary>
/// Provides functionality to determine which releases should be retained for each project and environment
/// according to the specified retention policy. An enriched result list is returned for each release that is kept.
/// 
/// <para>
/// The <see cref="ReleaseRetentionService"/> is responsible for applying a release retention rule
/// across all projects and environments. The rule is: for each project/environment combination,
/// keep the <c>N</c> most recently deployed releases, where <c>N</c> is specified by the <c>keepCount</c> parameter.
/// </para>
/// 
/// <para>
/// <b>Logging:</b> The service logs key steps, including validation issues, retention decisions, and summary statistics.
/// </para>
/// </summary>
public class ReleaseRetentionService : IReleaseRetentionService
{
    private readonly ILogger<ReleaseRetentionService> _logger;
    private readonly IDataProviderFactory _dataFactory;

    public ReleaseRetentionService(
        ILogger<ReleaseRetentionService> logger,
        IDataProviderFactory dataFactory)
    {
        _logger = logger;
        _dataFactory = dataFactory;
    }

    public async Task<IReadOnlyCollection<RetentionResult>> GetReleasesToKeepAsync(int keepCount)
    {
        // Validate keepCount parameter is a positive number
        if (keepCount <= 0)
        {
            throw new ArgumentException(
                "keepCount must be a positive number. The number of releases to keep per environment must be at least 1.",
                nameof(keepCount));
        }

        try
        {
            // Get the appropriate data provider based on configuration
            var dataProvider = await _dataFactory.GetDataProviderAsync();

            // Fetch all required data
            var projects = await dataProvider.GetProjectsAsync();
            var environments = await dataProvider.GetEnvironmentsAsync();
            var releases = await dataProvider.GetReleasesAsync();
            var deployments = await dataProvider.GetDeploymentsAsync();

            // Validate and prepare data as lookup dictionaries
            var validationResult = ValidateAndPrepareData(projects, environments, releases, deployments);

            // Process deployments and calculate release retention list
            var results = ProcessRetention(validationResult, keepCount);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing release retention.");
            throw;
        }
    }

    private ValidationResult ValidateAndPrepareData(
        IEnumerable<Project> projects,
        IEnumerable<Models.Environment> environments,
        IEnumerable<Release> releases,
        IEnumerable<Deployment> deployments)
    {
        // Ensure all data sources have at least one entry
        DataPreparationHelper.ValidateRequiredCollections(projects, environments, releases, deployments);

        // Create lookup dictionaries using the helper
        var projectsById = DataPreparationHelper.CreateLookupById(projects, p => p.Id, "Project", _logger);
        var environmentsById = DataPreparationHelper.CreateLookupById(environments, e => e.Id, "Environment", _logger);

        // Filter and validate releases
        var validReleases = releases
            .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Id))
            .Where(r => ValidationHelper.IsValidRelease(r, projectsById, _logger))
            .ToList();

        var releasesById = DataPreparationHelper.CreateLookupById(validReleases, r => r.Id, "Release", _logger);

        // Filter and validate deployments
        var validDeployments = deployments
            .Where(d => d != null && !string.IsNullOrWhiteSpace(d.Id))
            .Where(d => ValidationHelper.IsValidDeployment(d, releasesById, environmentsById, _logger))
            .ToList();

        var deploymentsById = DataPreparationHelper.CreateLookupById(validDeployments, d => d.Id, "Deployment", _logger);

        // Log invalid entries
        LoggingHelper.LogInvalidReleases(releases, projectsById, _logger);
        LoggingHelper.LogInvalidDeployments(deployments, releasesById, environmentsById, _logger);

        return new ValidationResult(projectsById, environmentsById, releasesById, deploymentsById);
    }

    private IReadOnlyCollection<RetentionResult> ProcessRetention(ValidationResult validationResult, int keepCount)
    {
        var results = new List<RetentionResult>();

        // Create a lookup of release to project ID for quick access
        var releaseToProjectMap = validationResult.ReleaseById
            .ToDictionary(r => r.Key, r => r.Value.ProjectId);

        // Group by project and environment combination using a tuple
        var deploymentGroups = validationResult.ValidDeployments
            .Select(d => (
                Deployment: d.Value,
                ProjectId: releaseToProjectMap[d.Value.ReleaseId]
            ))
            .GroupBy(d => new { d.ProjectId, d.Deployment.EnvironmentId });

        foreach (var group in deploymentGroups)
        {
            // grab the project and environment names, so we can enrich the results with them
            var projectName = validationResult.ProjectById.TryGetValue(group.Key.ProjectId, out var project)
                ? project.Name
                : group.Key.ProjectId;
            var environmentName = validationResult.EnvironmentById.TryGetValue(group.Key.EnvironmentId, out var environment)
                ? environment.Name
                : group.Key.EnvironmentId;

            // Get all releases for this project/environment combination, ordered by most recent deployment
            var allReleaseDeployments = group
                .GroupBy(d => d.Deployment.ReleaseId)
                .Select(g => new ReleaseDeploymentInfo(
                    ReleaseId: g.Key,
                    Release: validationResult.ReleaseById[g.Key],
                    LastDeployed: g.Min(d => d.Deployment.DeployedAt),
                    DeploymentCount: g.Count()
                ))
                .OrderByDescending(x => x.LastDeployed)
                .ThenByDescending(x => x.Release.CreatedAt)
                .ToList();

            // Log summary of releases found for this project/environment
            _logger.LogInformation(
                "Processing retention for Project '{ProjectName}' in Environment '{EnvironmentName}': " +
                "Found {TotalCount} releases",
                projectName, environmentName, allReleaseDeployments.Count);

            // Take only the releases we're keeping
            var releaseDeployments = allReleaseDeployments.Take(keepCount).ToList();

            // Log why each release is being kept
            foreach (var releaseInfo in releaseDeployments)
            {
                var rank = allReleaseDeployments.IndexOf(releaseInfo) + 1;
                var result = new RetentionResult(
                    ReleaseId: releaseInfo.ReleaseId,
                    ProjectId: group.Key.ProjectId,
                    ProjectName: projectName,
                    EnvironmentId: group.Key.EnvironmentId,
                    EnvironmentName: environmentName,
                    Version: releaseInfo.Release.Version!,
                    LastDeployedAt: releaseInfo.LastDeployed);

                results.Add(result);

                _logger.LogInformation(
                    "Keeping Release {ReleaseId} (Version: {Version}) for Project '{ProjectName}' in Environment '{EnvironmentName}' - " +
                    "Rank: {Rank}/{TotalCount}, Last deployed: {LastDeployed}, Deployment count: {DeploymentCount}, " +
                    "Reason: {Reason}",
                    releaseInfo.ReleaseId, 
                    releaseInfo.Release.Version, 
                    projectName, 
                    environmentName,
                    rank, 
                    allReleaseDeployments.Count, 
                    releaseInfo.LastDeployed, 
                    releaseInfo.DeploymentCount,
                    $"Within top {keepCount} most recently deployed releases");
            }

            // Log why releases are not being kept
            foreach (var releaseInfo in allReleaseDeployments.Skip(keepCount))
            {
                var rank = allReleaseDeployments.IndexOf(releaseInfo) + 1;
                _logger.LogInformation(
                    "Not keeping Release {ReleaseId} (Version: {Version}) for Project '{ProjectName}' in Environment '{EnvironmentName}' - " +
                    "Rank: {Rank}/{TotalCount}, Last deployed: {LastDeployed}, Deployment count: {DeploymentCount}, " +
                    "Reason: Outside top {KeepCount} most recently deployed releases",
                    releaseInfo.ReleaseId, 
                    releaseInfo.Release.Version, 
                    projectName, 
                    environmentName,
                    rank, 
                    allReleaseDeployments.Count, 
                    releaseInfo.LastDeployed, 
                    releaseInfo.DeploymentCount,
                    keepCount);
            }

            // Log summary of retention decisions
            _logger.LogInformation(
                "Completed retention processing for Project '{ProjectName}' in Environment '{EnvironmentName}': " +
                "Kept {KeptCount} of {TotalCount} releases",
                projectName, 
                environmentName, 
                releaseDeployments.Count, 
                allReleaseDeployments.Count);
        }

        // Log final summary across all projects and environments
        _logger.LogInformation(
            "Release retention processing completed. Found {ResultCount} releases to keep across {ProjectCount} projects and {EnvironmentCount} environments.",
            results.Count,
            validationResult.ProjectById.Count,
            validationResult.EnvironmentById.Count);

        return results;
    }
}
