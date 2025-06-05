using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Retention.Lib.Interfaces;
using Retention.Lib.Models;
using Microsoft.Extensions.Logging;
using Retention.Worker;
using Retention.Lib.Models.Configuration;
using Moq;

namespace Retention.Tests;

public class RetentionWorkerTests
{
    private readonly Mock<ILogger<RetentionWorker>> _loggerMock;
    private readonly Mock<IHostApplicationLifetime> _hostLifetimeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IReleaseRetentionService> _retentionServiceMock;
    private readonly RetentionOptions _config;
    private readonly IConfiguration _configuration;

    public RetentionWorkerTests()
    {
        _loggerMock = new Mock<ILogger<RetentionWorker>>();
        _hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _retentionServiceMock = new Mock<IReleaseRetentionService>();

        // Build configuration
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Setup service provider mocks
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);
        _serviceScopeFactoryMock.Setup(x => x.CreateScope())
            .Returns(_serviceScopeMock.Object);
        _serviceScopeMock.Setup(x => x.ServiceProvider)
            .Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IReleaseRetentionService)))
            .Returns(_retentionServiceMock.Object);

        // Setup retention service mock to return test data
        var testResults = new[]
        {
            new RetentionResult(
                ReleaseId: "Release-1",
                ProjectId: "Project-1",
                ProjectName: "Project One",
                EnvironmentId: "Environment-1",
                EnvironmentName: "Environment One",
                Version: "1.0.0",
                LastDeployedAt: DateTime.UtcNow)
        };
        _retentionServiceMock.Setup(x => x.GetReleasesToKeepAsync(It.IsAny<int>()))
            .ReturnsAsync(testResults);

        // Default config
        _config = new RetentionOptions
        {
            UseLocalData = true,
            KeepCount = 1,
            DataFiles = new DataFileOptions
            {
                Projects = "projects.json",
                Environments = "environments.json",
                Releases = "releases.json",
                Deployments = "deployments.json"
            }
        };
    }

    [Fact]
    public async Task StartAsync_WithLocalData_LoadsDataFromFiles()
    {
        // Arrange
        var worker = new RetentionWorker(
            _loggerMock.Object,
            _serviceProviderMock.Object,
            _config,
            _hostLifetimeMock.Object);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retention Worker execution completed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task StartAsync_WithDifferentKeepCounts_RespectsKeepCount(int keepCount)
    {
        // Arrange
        var config = new RetentionOptions
        {
            UseLocalData = true,
            KeepCount = keepCount,
            DataFiles = new DataFileOptions
            {
                Projects = "projects.json",
                Environments = "environments.json",
                Releases = "releases.json",
                Deployments = "deployments.json"
            }
        };

        var worker = new RetentionWorker(
            _loggerMock.Object,
            _serviceProviderMock.Object,
            config,
            _hostLifetimeMock.Object);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Releases to keep:")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithApiData_DoesNotLoadLocalFiles()
    {
        // Arrange
        var apiConfig = _configuration.GetSection("IntegrationTestAPI");
        var config = new RetentionOptions
        {
            UseLocalData = false,
            KeepCount = 1,
            DevOpsDeploy = new DevOpsDeployOptions
            {
                ApiBaseUrl = apiConfig["ApiBaseUrl"]!,
                SpaceId = apiConfig["SpaceId"]!,
                ApiKey = apiConfig["ApiKey"]!,
                ApiKeyHeader = apiConfig["ApiKeyHeader"]!,
                TimeoutSeconds = int.Parse(apiConfig["TimeoutSeconds"]!)
            }
        };

        var worker = new RetentionWorker(
            _loggerMock.Object,
            _serviceProviderMock.Object,
            config,
            _hostLifetimeMock.Object);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retention Worker execution completed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenErrorOccurs_LogsErrorAndThrows()
    {
        // Arrange
        var config = new RetentionOptions
        {
            UseLocalData = true,
            KeepCount = 1,
            DataFiles = new DataFileOptions
            {
                Projects = "test-projects.json",
                Environments = "test-environments.json",
                Releases = "test-releases.json",
                Deployments = "test-deployments.json"
            }
        };

        _retentionServiceMock.Setup(x => x.GetReleasesToKeepAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Test error"));

        var worker = new RetentionWorker(
            _loggerMock.Object,
            _serviceProviderMock.Object,
            config,
            _hostLifetimeMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => worker.StartAsync(CancellationToken.None));
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An error occurred during retention analysis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
} 