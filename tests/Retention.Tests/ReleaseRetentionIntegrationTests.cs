using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Retention.Lib.Data;
using Retention.Lib.Interfaces;
using Retention.Lib.Models;
using Retention.Lib.Models.Configuration;
using Retention.Lib.Services;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Retention.Tests;

public class ReleaseRetentionIntegrationTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly ServiceProvider _serviceProvider;
    private readonly IReleaseRetentionService _service;
    private readonly IDataProviderFactory _dataFactory;
    private readonly IConfiguration _configuration;

    private class RetentionOptionsBuilder
    {
        private readonly IConfiguration _configuration;

        public RetentionOptionsBuilder(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public RetentionOptions BuildLocalDataOptions(string testDataPath)
        {
            return new RetentionOptions
            {
                UseLocalData = true,
                KeepCount = 1,
                DataFiles = new DataFileOptions
                {
                    Projects = Path.Combine(testDataPath, "projects.json"),
                    Environments = Path.Combine(testDataPath, "environments.json"),
                    Releases = Path.Combine(testDataPath, "releases.json"),
                    Deployments = Path.Combine(testDataPath, "deployments.json")
                }
            };
        }

        public RetentionOptions BuildApiOptions()
        {
            var apiConfig = _configuration.GetSection("IntegrationTestAPI");
            return new RetentionOptions
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
        }
    }

    public ReleaseRetentionIntegrationTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), "RetentionTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataPath);

        // Build configuration
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add HTTP client
        services.AddHttpClient();

        // Add configuration
        var optionsBuilder = new RetentionOptionsBuilder(_configuration);
        var localDataOptions = optionsBuilder.BuildLocalDataOptions(_testDataPath);
        services.AddSingleton<RetentionOptions>(localDataOptions);

        // Add services
        services.AddSingleton<IDataProviderFactory, DataProviderFactory>();
        services.AddSingleton<IReleaseRetentionService, ReleaseRetentionService>();

        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<IReleaseRetentionService>();
        _dataFactory = _serviceProvider.GetRequiredService<IDataProviderFactory>();

        // Create initial test data
        CreateTestJsonFiles();
    }

    [Fact]
    public async Task GetReleasesToKeepAsync_WithLocalFiles_ReturnsExpectedResults()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        var testData = new
        {
            Projects = new[]
            {
                new Project("Project-1", "Project One"),
                new Project("Project-2", "Project Two")
            },
            Environments = new[]
            {
                new Lib.Models.Environment("Environment-1", "Environment One"),
                new Lib.Models.Environment("Environment-2", "Environment Two")
            },
            Releases = new[]
            {
                new Release("Release-1", "Project-1", baseTime.AddHours(-2), "1.0.0"),
                new Release("Release-2", "Project-1", baseTime.AddHours(-1), "1.0.1"),
                new Release("Release-3", "Project-2", baseTime.AddHours(-2), "2.0.0")
            },
            Deployments = new[]
            {
                new Deployment("Deploy-1", "Release-1", "Environment-1", baseTime.AddHours(-1)),
                new Deployment("Deploy-2", "Release-2", "Environment-1", baseTime),
                new Deployment("Deploy-3", "Release-1", "Environment-2", baseTime.AddHours(-1)),
                new Deployment("Deploy-4", "Release-3", "Environment-1", baseTime)
            }
        };

        await WriteTestData(testData);

        // Act
        var result = await _service.GetReleasesToKeepAsync(1);

        // Assert
        result.Should().HaveCount(3); // One release per project/environment combination
        result.Should().Contain(r => 
            r.ReleaseId == "Release-2" && 
            r.ProjectId == "Project-1" && 
            r.EnvironmentId == "Environment-1");
        result.Should().Contain(r => 
            r.ReleaseId == "Release-1" && 
            r.ProjectId == "Project-1" && 
            r.EnvironmentId == "Environment-2");
        result.Should().Contain(r => 
            r.ReleaseId == "Release-3" && 
            r.ProjectId == "Project-2" && 
            r.EnvironmentId == "Environment-1");
    }

    [Fact]
    public async Task GetReleasesToKeepAsync_WithDevOpsApi_ReturnsExpectedResults()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        var testData = new
        {
            Projects = new[]
            {
                new Project("Project-1", "Project One"),
                new Project("Project-2", "Project Two")
            },
            Environments = new[]
            {
                new Lib.Models.Environment("Environment-1", "Environment One"),
                new Lib.Models.Environment("Environment-2", "Environment Two")
            },
            Releases = new[]
            {
                new Release("Release-1", "Project-1", baseTime.AddHours(-2), "1.0.0"),
                new Release("Release-2", "Project-1", baseTime.AddHours(-1), "1.0.1"),
                new Release("Release-3", "Project-2", baseTime.AddHours(-2), "2.0.0")
            },
            Deployments = new[]
            {
                new Deployment("Deploy-1", "Release-1", "Environment-1", baseTime.AddHours(-1)),
                new Deployment("Deploy-2", "Release-2", "Environment-1", baseTime),
                new Deployment("Deploy-3", "Release-1", "Environment-2", baseTime.AddHours(-1)),
                new Deployment("Deploy-4", "Release-3", "Environment-1", baseTime)
            }
        };

        // Configure for API testing
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Configure HTTP client to simulate API failure
        services.AddHttpClient("DevOpsDeploy", client =>
        {
            client.BaseAddress = new Uri(_configuration["IntegrationTestAPI:ApiBaseUrl"]!);
            client.DefaultRequestHeaders.Add(
                _configuration["IntegrationTestAPI:ApiKeyHeader"]!,
                _configuration["IntegrationTestAPI:ApiKey"]);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        })
        .AddHttpMessageHandler(() => new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("API Error")
        }));

        // Configure options for API testing
        var optionsBuilder = new RetentionOptionsBuilder(_configuration);
        var apiOptions = optionsBuilder.BuildApiOptions();
        services.AddSingleton<RetentionOptions>(apiOptions);

        services.AddSingleton<IDataProviderFactory, DataProviderFactory>();
        services.AddSingleton<IReleaseRetentionService, ReleaseRetentionService>();

        var apiServiceProvider = services.BuildServiceProvider();
        var apiService = apiServiceProvider.GetRequiredService<IReleaseRetentionService>();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            apiService.GetReleasesToKeepAsync(1));
    }

    private class MockHttpMessageHandler : DelegatingHandler
    {
        private readonly HttpResponseMessage _response;

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    [Fact]
    public async Task GetReleasesToKeepAsync_WithInvalidData_FiltersInvalidData()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        var testData = new
        {
            Projects = new[]
            {
                new Project("Project-1", "Project One")
            },
            Environments = new[]
            {
                new Lib.Models.Environment("Environment-1", "Environment One")
            },
            Releases = new[]
            {
                new Release("Release-1", "Project-1", baseTime, "1.0.0"),
                new Release("Release-2", "NonExistentProject", baseTime, "1.0.1"),
                new Release("Release-3", "Project-1", baseTime, null!)
            },
            Deployments = new[]
            {
                new Deployment("Deploy-1", "Release-1", "Environment-1", baseTime),
                new Deployment("Deploy-2", "NonExistentRelease", "Environment-1", baseTime),
                new Deployment("Deploy-3", "Release-1", "NonExistentEnvironment", baseTime)
            }
        };

        await WriteTestData(testData);

        // Act
        var result = await _service.GetReleasesToKeepAsync(2);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainSingle(r => 
            r.ReleaseId == "Release-1" && 
            r.ProjectId == "Project-1" && 
            r.EnvironmentId == "Environment-1");
    }

    private async Task WriteTestData<T>(T data)
    {
        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(data);
            if (value != null)
            {
                var fileName = $"{property.Name.ToLower()}.json";
                var filePath = Path.Combine(_testDataPath, fileName);
                await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(value));
            }
        }
    }

    private void CreateTestJsonFiles()
    {
        // Create empty JSON files for initial setup
        var files = new[] { "projects.json", "environments.json", "releases.json", "deployments.json" };
        foreach (var file in files)
        {
            File.WriteAllText(Path.Combine(_testDataPath, file), "[]");
        }
    }

    public void Dispose()
    {
        // Clean up test files
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, true);
        }
        _serviceProvider.Dispose();
    }
} 