using System.Text.Json;
using Retention.Lib.Interfaces;
using Retention.Lib.Models;
using Retention.Lib.Models.Configuration;

namespace Retention.Lib.Data;


/// <summary>
/// Provides data access by reading JSON files from disk for projects, environments, releases, and deployments.
/// Converts relative file paths to absolute paths based on the application's base directory.
/// Implements <see cref="IDataProvider"/> to supply collections of domain models from JSON data sources.
/// </summary>
public class JsonFileDataProvider : IDataProvider
{
    private readonly string _projectsPath;
    private readonly string _envsPath;
    private readonly string _releasesPath;
    private readonly string _deploymentsPath;

    public JsonFileDataProvider(DataFileOptions options)
    {
        // Get the application's base directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Convert relative paths to absolute paths (defensive pathing)
        _projectsPath = Path.GetFullPath(Path.Combine(baseDir, options.Projects));
        _envsPath = Path.GetFullPath(Path.Combine(baseDir, options.Environments));
        _releasesPath = Path.GetFullPath(Path.Combine(baseDir, options.Releases));
        _deploymentsPath = Path.GetFullPath(Path.Combine(baseDir, options.Deployments));
    }

    // Stream data from JSON files asynchronously, accommodating potential large datasets
    public async Task<IEnumerable<Project>> GetProjectsAsync()
    {
        await using var stream = File.OpenRead(_projectsPath);
        return (await JsonSerializer.DeserializeAsync<List<Project>>(stream))
               ?? Enumerable.Empty<Project>();
    }

    public async Task<IEnumerable<Models.Environment>> GetEnvironmentsAsync()
    {
        await using var stream = File.OpenRead(_envsPath);
        return (await JsonSerializer.DeserializeAsync<List<Models.Environment>>(stream))
               ?? Enumerable.Empty<Models.Environment>();
    }

    public async Task<IEnumerable<Release>> GetReleasesAsync()
    {
        await using var stream = File.OpenRead(_releasesPath);
        return (await JsonSerializer.DeserializeAsync<List<Release>>(stream))
               ?? Enumerable.Empty<Release>();
    }

    public async Task<IEnumerable<Deployment>> GetDeploymentsAsync()
    {
        await using var stream = File.OpenRead(_deploymentsPath);
        return (await JsonSerializer.DeserializeAsync<List<Deployment>>(stream))
               ?? Enumerable.Empty<Deployment>();
    }
}
