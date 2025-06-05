using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Retention.Lib.Data;
using Retention.Lib.Models;
using Retention.Lib.Models.Configuration;

namespace Retention.Tests;

public class JsonFileDataProviderTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly JsonFileDataProvider _provider;
    private readonly Mock<ILogger<JsonFileDataProvider>> _loggerMock;

    public JsonFileDataProviderTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), "RetentionTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataPath);
        
        _loggerMock = new Mock<ILogger<JsonFileDataProvider>>();
        
        var options = new DataFileOptions
        {
            Projects = Path.Combine(_testDataPath, "projects.json"),
            Environments = Path.Combine(_testDataPath, "environments.json"),
            Releases = Path.Combine(_testDataPath, "releases.json"),
            Deployments = Path.Combine(_testDataPath, "deployments.json")
        };
        
        _provider = new JsonFileDataProvider(options);
        
        // Create initial test data
        CreateTestJsonFiles();
    }

    [Fact]
    public async Task GetProjectsAsync_ValidJsonFile_ReturnsProjects()
    {
        // Arrange
        var expectedProjects = new[]
        {
            new Project("Project-1", "Project One"),
            new Project("Project-2", "Project Two")
        };
        await File.WriteAllTextAsync(
            Path.Combine(_testDataPath, "projects.json"),
            JsonSerializer.Serialize(expectedProjects));

        // Act
        var result = await _provider.GetProjectsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedProjects);
    }

    [Fact]
    public async Task GetEnvironmentsAsync_ValidJsonFile_ReturnsEnvironments()
    {
        // Arrange
        var expectedEnvironments = new[]
        {
            new Lib.Models.Environment("Environment-1", "Environment One"),
            new Lib.Models.Environment("Environment-2", "Environment Two")
        };
        await File.WriteAllTextAsync(
            Path.Combine(_testDataPath, "environments.json"),
            JsonSerializer.Serialize(expectedEnvironments));

        // Act
        var result = await _provider.GetEnvironmentsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedEnvironments);
    }

    [Fact]
    public async Task GetReleasesAsync_ValidJsonFile_ReturnsReleases()
    {
        // Arrange
        var expectedReleases = new[]
        {
            new Release("Release-1", "Project-1", DateTime.UtcNow, "1.0.0"),
            new Release("Release-2", "Project-1", DateTime.UtcNow, "1.0.1")
        };
        await File.WriteAllTextAsync(
            Path.Combine(_testDataPath, "releases.json"),
            JsonSerializer.Serialize(expectedReleases));

        // Act
        var result = await _provider.GetReleasesAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedReleases);
    }

    [Fact]
    public async Task GetDeploymentsAsync_ValidJsonFile_ReturnsDeployments()
    {
        // Arrange
        var expectedDeployments = new[]
        {
            new Deployment("Deploy-1", "Release-1", "Environment-1", DateTime.UtcNow),
            new Deployment("Deploy-2", "Release-2", "Environment-1", DateTime.UtcNow)
        };
        await File.WriteAllTextAsync(
            Path.Combine(_testDataPath, "deployments.json"),
            JsonSerializer.Serialize(expectedDeployments));

        // Act
        var result = await _provider.GetDeploymentsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedDeployments);
    }

    [Fact]
    public async Task GetProjectsAsync_InvalidJsonFile_ThrowsJsonException()
    {
        // Arrange
        await File.WriteAllTextAsync(
            Path.Combine(_testDataPath, "projects.json"),
            "invalid json");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => _provider.GetProjectsAsync());
    }

    [Fact]
    public async Task GetProjectsAsync_MissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        File.Delete(Path.Combine(_testDataPath, "projects.json"));

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _provider.GetProjectsAsync());
    }

    [Fact]
    public async Task GetProjectsAsync_EmptyFile_ReturnsEmptyCollection()
    {
        // Arrange
        await File.WriteAllTextAsync(
            Path.Combine(_testDataPath, "projects.json"),
            "[]");

        // Act
        var result = await _provider.GetProjectsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectsAsync_NullValuesInJson_ReturnsNullValues()
    {
        // Arrange
        var projects = new[]
        {
            new Project("Project-1", null!),
            new Project(null!, "Project Two")
        };
        await File.WriteAllTextAsync(
            Path.Combine(_testDataPath, "projects.json"),
            JsonSerializer.Serialize(projects));

        // Act
        var result = await _provider.GetProjectsAsync();

        // Assert
        result.Should().BeEquivalentTo(projects);
    }

    [Fact]
    public async Task GetProjectsAsync_FileInUse_ThrowsIOException()
    {
        // Arrange
        using var fileStream = File.Open(
            Path.Combine(_testDataPath, "projects.json"),
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None);

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(() => _provider.GetProjectsAsync());
    }

    private void CreateTestJsonFiles()
    {
        // Create sample JSON files for testing
        var projects = new[]
        {
            new Project("Project-1", "Project One"),
            new Project("Project-2", "Project Two")
        };
        File.WriteAllText(
            Path.Combine(_testDataPath, "projects.json"),
            JsonSerializer.Serialize(projects));

        var environments = new[]
        {
            new Lib.Models.Environment("Environment-1", "Environment One"),
            new Lib.Models.Environment("Environment-2", "Environment Two")
        };
        File.WriteAllText(
            Path.Combine(_testDataPath, "environments.json"),
            JsonSerializer.Serialize(environments));

        var releases = new[]
        {
            new Release("Release-1", "Project-1", DateTime.UtcNow, "1.0.0"),
            new Release("Release-2", "Project-1", DateTime.UtcNow, "1.0.1")
        };
        File.WriteAllText(
            Path.Combine(_testDataPath, "releases.json"),
            JsonSerializer.Serialize(releases));

        var deployments = new[]
        {
            new Deployment("Deploy-1", "Release-1", "Environment-1", DateTime.UtcNow),
            new Deployment("Deploy-2", "Release-2", "Environment-1", DateTime.UtcNow)
        };
        File.WriteAllText(
            Path.Combine(_testDataPath, "deployments.json"),
            JsonSerializer.Serialize(deployments));
    }

    public void Dispose()
    {
        // Clean up test files
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, true);
        }
    }
} 