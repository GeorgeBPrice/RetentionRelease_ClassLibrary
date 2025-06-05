using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Retention.Lib.Clients;
using Retention.Lib.Models;
using Retention.Lib.Models.Configuration;

namespace Retention.Tests;

public class DevOpsDeployClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly DevOpsDeployOptions _options;
    private readonly DevOpsDeployClient _client;
    private readonly Mock<ILogger<DevOpsDeployClient>> _loggerMock;
    private readonly IConfiguration _configuration;

    public DevOpsDeployClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        // Build configuration
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var apiConfig = _configuration.GetSection("IntegrationTestAPI");
        _options = new DevOpsDeployOptions
        {
            ApiBaseUrl = apiConfig["ApiBaseUrl"]!,
            SpaceId = apiConfig["SpaceId"]!,
            ApiKey = apiConfig["ApiKey"]!,
            ApiKeyHeader = apiConfig["ApiKeyHeader"]!,
            TimeoutSeconds = int.Parse(apiConfig["TimeoutSeconds"]!)
        };

        _httpClient.BaseAddress = new Uri(_options.ApiBaseUrl);
        _loggerMock = new Mock<ILogger<DevOpsDeployClient>>();
        _client = new DevOpsDeployClient(_httpClient, _options, _loggerMock.Object);
    }

    [Fact]
    public async Task GetProjectsAsync_ValidResponse_ReturnsProjects()
    {
        // Arrange
        var expectedProjects = new[]
        {
            new Project("Project-1", "Project One"),
            new Project("Project-2", "Project Two")
        };

        SetupHttpResponse<Project[]>(HttpStatusCode.OK, expectedProjects);

        // Act
        var result = await _client.GetProjectsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedProjects);
        VerifyHttpRequest("projects");
    }

    [Fact]
    public async Task GetEnvironmentsAsync_ValidResponse_ReturnsEnvironments()
    {
        // Arrange
        var expectedEnvironments = new[]
        {
            new Lib.Models.Environment("Environment-1", "Environment One"),
            new Lib.Models.Environment("Environment-2", "Environment Two")
        };

        SetupHttpResponse<Lib.Models.Environment[]>(HttpStatusCode.OK, expectedEnvironments);

        // Act
        var result = await _client.GetEnvironmentsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedEnvironments);
        VerifyHttpRequest("environments");
    }

    [Fact]
    public async Task GetReleasesAsync_ValidResponse_ReturnsReleases()
    {
        // Arrange
        var expectedReleases = new[]
        {
            new Release("Release-1", "Project-1", DateTime.UtcNow, "1.0.0"),
            new Release("Release-2", "Project-1", DateTime.UtcNow, "1.0.1")
        };

        SetupHttpResponse<Release[]>(HttpStatusCode.OK, expectedReleases);

        // Act
        var result = await _client.GetReleasesAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedReleases);
        VerifyHttpRequest("releases");
    }

    [Fact]
    public async Task GetDeploymentsAsync_ValidResponse_ReturnsDeployments()
    {
        // Arrange
        var expectedDeployments = new[]
        {
            new Deployment("Deploy-1", "Release-1", "Environment-1", DateTime.UtcNow),
            new Deployment("Deploy-2", "Release-2", "Environment-1", DateTime.UtcNow)
        };

        SetupHttpResponse<Deployment[]>(HttpStatusCode.OK, expectedDeployments);

        // Act
        var result = await _client.GetDeploymentsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedDeployments);
        VerifyHttpRequest("deployments");
    }

    [Fact]
    public async Task GetProjectsAsync_ApiError_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpResponse<Project[]>(HttpStatusCode.InternalServerError, null);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetProjectsAsync());
    }

    [Fact]
    public async Task GetProjectsAsync_EmptyResponse_ReturnsEmptyCollection()
    {
        // Arrange
        SetupHttpResponse<Project[]>(HttpStatusCode.OK, Array.Empty<Project>());

        // Act
        var result = await _client.GetProjectsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectsAsync_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("invalid json")
            });

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => _client.GetProjectsAsync());
    }

    private void SetupHttpResponse<T>(HttpStatusCode statusCode, T? content)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = content != null ? JsonContent.Create(content) : null
            });
    }

    private void VerifyHttpRequest(string expectedPath)
    {
        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.AbsolutePath.EndsWith($"/api/{_options.SpaceId}/{expectedPath}")),
                ItExpr.IsAny<CancellationToken>());
    }
} 