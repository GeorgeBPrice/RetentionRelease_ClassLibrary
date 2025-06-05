using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Retention.Lib.Interfaces;
using Retention.Lib.Models;
using Retention.Lib.Services;

namespace Retention.Tests;

public class ReleaseRetentionServiceTests
{
    private readonly IReleaseRetentionService _svc;
    private readonly MockDataProviderFactory _dataFactory;
    private readonly Mock<ILogger<ReleaseRetentionService>> _loggerMock;

    public ReleaseRetentionServiceTests()
    {
        _loggerMock = new Mock<ILogger<ReleaseRetentionService>>();
        _dataFactory = new MockDataProviderFactory();
        _svc = new ReleaseRetentionService(
            _loggerMock.Object,
            _dataFactory);
    }

    // Core retention logic test cases
    [Fact]
    public async Task TestCase1_OneRelease_KeepOne()
    {
        // Arrange
        _dataFactory.ConfigureMockData(
            projects: new[] { new Project("Project-1", "Project-1") },
            environments: new[] { new Lib.Models.Environment("Environment-1", "Environment-1") },
            releases: new[]
            {
                new Release("Release-1", "Project-1", DateTime.Parse("2000-01-01T08:00:00"), "1.0.0")
            },
            deployments: new[]
            {
                new Deployment("Deploy-1", "Release-1", "Environment-1", DateTime.Parse("2000-01-01T10:00:00"))
            });

        // Act
        var toKeep = await _svc.GetReleasesToKeepAsync(keepCount: 1);

        // Assert
        toKeep.Should().BeEquivalentTo(new[]
        {
            new RetentionResult(
                ReleaseId: "Release-1",
                ProjectId: "Project-1",
                ProjectName: "Project-1",
                EnvironmentId: "Environment-1",
                EnvironmentName: "Environment-1",
                Version: "1.0.0",
                LastDeployedAt: DateTime.Parse("2000-01-01T10:00:00"))
        });
    }

    [Fact]
    public async Task TestCase2_TwoReleasesSameEnv_KeepOne()
    {
        // Arrange
        _dataFactory.ConfigureMockData(
            projects: new[] { new Project("Project-1", "Project-1") },
            environments: new[] { new Lib.Models.Environment("Environment-1", "Environment-1") },
            releases: new[]
            {
                new Release("Release-1", "Project-1", DateTime.Parse("2000-01-01T08:00:00"), "1.0.0"),
                new Release("Release-2", "Project-1", DateTime.Parse("2000-01-01T09:00:00"), "1.0.1")
            },
            deployments: new[]
            {
                new Deployment("Deploy-1", "Release-1", "Environment-1", DateTime.Parse("2000-01-01T11:00:00")),
                new Deployment("Deploy-2", "Release-2", "Environment-1", DateTime.Parse("2000-01-01T10:00:00"))
            });

        // Act
        var toKeep = await _svc.GetReleasesToKeepAsync(keepCount: 1);

        // Assert
        toKeep.Should().BeEquivalentTo(new[]
        {
            new RetentionResult(
                ReleaseId: "Release-1",
                ProjectId: "Project-1",
                ProjectName: "Project-1",
                EnvironmentId: "Environment-1",
                EnvironmentName: "Environment-1",
                Version: "1.0.0",
                LastDeployedAt: DateTime.Parse("2000-01-01T11:00:00"))
        });
    }

    [Fact]
    public async Task TestCase3_TwoReleasesDifferentEnvs_KeepOnePerEnv()
    {
        // Arrange
        _dataFactory.ConfigureMockData(
            projects: new[] { new Project("Project-1", "Project-1") },
            environments: new[]
            {
                new Lib.Models.Environment("Environment-1", "Environment-1"),
                new Lib.Models.Environment("Environment-2", "Environment-2")
            },
            releases: new[]
            {
                new Release("Release-1", "Project-1", DateTime.Parse("2000-01-01T08:00:00"), "1.0.0"),
                new Release("Release-2", "Project-1", DateTime.Parse("2000-01-01T09:00:00"), "1.0.1")
            },
            deployments: new[]
            {
                new Deployment("Deploy-1", "Release-1", "Environment-2", DateTime.Parse("2000-01-02T11:00:00")),
                new Deployment("Deploy-2", "Release-2", "Environment-1", DateTime.Parse("2000-01-01T10:00:00"))
            });

        // Act
        var toKeep = await _svc.GetReleasesToKeepAsync(keepCount: 1);

        // Assert
        toKeep.Should().BeEquivalentTo(new[]
        {
            new RetentionResult(
                ReleaseId: "Release-1",
                ProjectId: "Project-1",
                ProjectName: "Project-1",
                EnvironmentId: "Environment-2",
                EnvironmentName: "Environment-2",
                Version: "1.0.0",
                LastDeployedAt: DateTime.Parse("2000-01-02T11:00:00")),
            new RetentionResult(
                ReleaseId: "Release-2",
                ProjectId: "Project-1",
                ProjectName: "Project-1",
                EnvironmentId: "Environment-1",
                EnvironmentName: "Environment-1",
                Version: "1.0.1",
                LastDeployedAt: DateTime.Parse("2000-01-01T10:00:00"))
        });
    }

    [Fact]
    public async Task TestCase4_MultipleProjectsAndEnvs_KeepTwo()
    {
        // Arrange
        _dataFactory.ConfigureMockData(
            projects: new[]
            {
                new Project("Project-1", "Project-1"),
                new Project("Project-2", "Project-2")
            },
            environments: new[]
            {
                new Lib.Models.Environment("Environment-1", "Environment-1"),
                new Lib.Models.Environment("Environment-2", "Environment-2")
            },
            releases: new[]
            {
                new Release("Release-1", "Project-1", DateTime.Parse("2000-01-01T08:00:00"), "1.0.0"),
                new Release("Release-2", "Project-1", DateTime.Parse("2000-01-01T09:00:00"), "1.0.1"),
                new Release("Release-3", "Project-1", DateTime.Parse("2000-01-01T10:00:00"), "1.0.2"),
                new Release("Release-4", "Project-2", DateTime.Parse("2000-01-01T08:00:00"), "1.0.0"),
                new Release("Release-5", "Project-2", DateTime.Parse("2000-01-01T09:00:00"), "1.0.1")
            },
            deployments: new[]
            {
                // Project-1, Environment-1 deployments
                new Deployment("Deploy-1", "Release-1", "Environment-1", DateTime.Parse("2000-01-01T10:00:00")),
                new Deployment("Deploy-2", "Release-2", "Environment-1", DateTime.Parse("2000-01-01T11:00:00")),
                new Deployment("Deploy-3", "Release-3", "Environment-1", DateTime.Parse("2000-01-01T12:00:00")),
                // Project-1, Environment-2 deployments
                new Deployment("Deploy-4", "Release-1", "Environment-2", DateTime.Parse("2000-01-02T10:00:00")),
                new Deployment("Deploy-5", "Release-2", "Environment-2", DateTime.Parse("2000-01-02T11:00:00")),
                // Project-2, Environment-1 deployments
                new Deployment("Deploy-6", "Release-4", "Environment-1", DateTime.Parse("2000-01-01T10:00:00")),
                new Deployment("Deploy-7", "Release-5", "Environment-1", DateTime.Parse("2000-01-01T11:00:00")),
                // Project-2, Environment-2 deployments
                new Deployment("Deploy-8", "Release-4", "Environment-2", DateTime.Parse("2000-01-02T10:00:00"))
            });

        // Act
        var toKeep = await _svc.GetReleasesToKeepAsync(keepCount: 2);

        // Assert
        toKeep.Should().BeEquivalentTo(new[]
        {
            // Project-1, Environment-1: Keep Release-2 and Release-3 (most recent)
            new RetentionResult(
                ReleaseId: "Release-2",
                ProjectId: "Project-1",
                ProjectName: "Project-1",
                EnvironmentId: "Environment-1",
                EnvironmentName: "Environment-1",
                Version: "1.0.1",
                LastDeployedAt: DateTime.Parse("2000-01-01T11:00:00")),
            new RetentionResult(
                ReleaseId: "Release-3",
                ProjectId: "Project-1",
                ProjectName: "Project-1",
                EnvironmentId: "Environment-1",
                EnvironmentName: "Environment-1",
                Version: "1.0.2",
                LastDeployedAt: DateTime.Parse("2000-01-01T12:00:00")),
            // Project-1, Environment-2: Keep Release-1 and Release-2 (only two deployed)
            new RetentionResult(
                ReleaseId: "Release-1",
                ProjectId: "Project-1",
                ProjectName: "Project-1",
                EnvironmentId: "Environment-2",
                EnvironmentName: "Environment-2",
                Version: "1.0.0",
                LastDeployedAt: DateTime.Parse("2000-01-02T10:00:00")),
            new RetentionResult(
                ReleaseId: "Release-2",
                ProjectId: "Project-1",
                ProjectName: "Project-1",
                EnvironmentId: "Environment-2",
                EnvironmentName: "Environment-2",
                Version: "1.0.1",
                LastDeployedAt: DateTime.Parse("2000-01-02T11:00:00")),
            // Project-2, Environment-1: Keep Release-4 and Release-5 (only two deployed)
            new RetentionResult(
                ReleaseId: "Release-4",
                ProjectId: "Project-2",
                ProjectName: "Project-2",
                EnvironmentId: "Environment-1",
                EnvironmentName: "Environment-1",
                Version: "1.0.0",
                LastDeployedAt: DateTime.Parse("2000-01-01T10:00:00")),
            new RetentionResult(
                ReleaseId: "Release-5",
                ProjectId: "Project-2",
                ProjectName: "Project-2",
                EnvironmentId: "Environment-1",
                EnvironmentName: "Environment-1",
                Version: "1.0.1",
                LastDeployedAt: DateTime.Parse("2000-01-01T11:00:00")),
            // Project-2, Environment-2: Keep Release-4 (only one deployed)
            new RetentionResult(
                ReleaseId: "Release-4",
                ProjectId: "Project-2",
                ProjectName: "Project-2",
                EnvironmentId: "Environment-2",
                EnvironmentName: "Environment-2",
                Version: "1.0.0",
                LastDeployedAt: DateTime.Parse("2000-01-02T10:00:00"))
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task TestCase5_MultipleReleases_DifferentKeepCounts(int keepCount)
    {
        // Arrange
        _dataFactory.ConfigureMockData(
            projects: new[] { new Project("Project-1", "Project-1") },
            environments: new[] { new Lib.Models.Environment("Environment-1", "Environment-1") },
            releases: new[]
            {
                new Release("Release-1", "Project-1", DateTime.Parse("2000-01-01T08:00:00"), "1.0.0"),
                new Release("Release-2", "Project-1", DateTime.Parse("2000-01-01T09:00:00"), "1.0.1"),
                new Release("Release-3", "Project-1", DateTime.Parse("2000-01-01T10:00:00"), "1.0.2"),
                new Release("Release-4", "Project-1", DateTime.Parse("2000-01-01T11:00:00"), "1.0.3"),
                new Release("Release-5", "Project-1", DateTime.Parse("2000-01-01T12:00:00"), "1.0.4")
            },
            deployments: new[]
            {
                new Deployment("Deploy-1", "Release-1", "Environment-1", DateTime.Parse("2000-01-01T13:00:00")),
                new Deployment("Deploy-2", "Release-2", "Environment-1", DateTime.Parse("2000-01-01T14:00:00")),
                new Deployment("Deploy-3", "Release-3", "Environment-1", DateTime.Parse("2000-01-01T15:00:00")),
                new Deployment("Deploy-4", "Release-4", "Environment-1", DateTime.Parse("2000-01-01T16:00:00")),
                new Deployment("Deploy-5", "Release-5", "Environment-1", DateTime.Parse("2000-01-01T17:00:00"))
            });

        // Act
        var toKeep = await _svc.GetReleasesToKeepAsync(keepCount);

        // Assert
        toKeep.Should().HaveCount(keepCount);
        toKeep.Should().BeInDescendingOrder(r => r.LastDeployedAt);
        toKeep.Should().AllSatisfy(r => r.ProjectId.Should().Be("Project-1"));
        toKeep.Should().AllSatisfy(r => r.EnvironmentId.Should().Be("Environment-1"));
    }

    // Edge cases for retention logic
    [Fact]
    public async Task TestCase8_KeepCountGreaterThanReleases_ReturnsAllReleases()
    {
        // Arrange
        _dataFactory.ConfigureMockData(
            projects: new[] { new Project("Project-1", "Project-1") },
            environments: new[] { new Lib.Models.Environment("Environment-1", "Environment-1") },
            releases: new[]
            {
                new Release("Release-1", "Project-1", DateTime.Parse("2000-01-01T08:00:00"), "1.0.0"),
                new Release("Release-2", "Project-1", DateTime.Parse("2000-01-01T09:00:00"), "1.0.1")
            },
            deployments: new[]
            {
                new Deployment("Deploy-1", "Release-1", "Environment-1", DateTime.Parse("2000-01-01T10:00:00")),
                new Deployment("Deploy-2", "Release-2", "Environment-1", DateTime.Parse("2000-01-01T11:00:00"))
            });

        // Act
        var toKeep = await _svc.GetReleasesToKeepAsync(keepCount: 5);

        // Assert
        toKeep.Should().HaveCount(2);
        toKeep.Should().BeInDescendingOrder(r => r.LastDeployedAt);
    }

    [Fact]
    public async Task TestCase9_MultipleEnvironments_DifferentDeploymentPatterns()
    {
        // Arrange
        _dataFactory.ConfigureMockData(
            projects: new[] { new Project("Project-1", "Project-1") },
            environments: new[]
            {
                new Lib.Models.Environment("Environment-1", "Environment-1"),
                new Lib.Models.Environment("Environment-2", "Environment-2"),
                new Lib.Models.Environment("Environment-3", "Environment-3")
            },
            releases: new[]
            {
                new Release("Release-1", "Project-1", DateTime.Parse("2000-01-01T08:00:00"), "1.0.0"),
                new Release("Release-2", "Project-1", DateTime.Parse("2000-01-01T09:00:00"), "1.0.1"),
                new Release("Release-3", "Project-1", DateTime.Parse("2000-01-01T10:00:00"), "1.0.2")
            },
            deployments: new[]
            {
                // Environment-1: All releases deployed
                new Deployment("Deploy-1", "Release-1", "Environment-1", DateTime.Parse("2000-01-01T11:00:00")),
                new Deployment("Deploy-2", "Release-2", "Environment-1", DateTime.Parse("2000-01-01T12:00:00")),
                new Deployment("Deploy-3", "Release-3", "Environment-1", DateTime.Parse("2000-01-01T13:00:00")),
                // Environment-2: Only Release-1 and Release-2 deployed
                new Deployment("Deploy-4", "Release-1", "Environment-2", DateTime.Parse("2000-01-01T14:00:00")),
                new Deployment("Deploy-5", "Release-2", "Environment-2", DateTime.Parse("2000-01-01T15:00:00")),
                // Environment-3: Only Release-1 deployed
                new Deployment("Deploy-6", "Release-1", "Environment-3", DateTime.Parse("2000-01-01T16:00:00"))
            });

        // Act
        var toKeep = await _svc.GetReleasesToKeepAsync(keepCount: 2);

        // Assert
        toKeep.Should().HaveCount(5); // 2 for Env1, 2 for Env2, 1 for Env3
        toKeep.Should().Contain(r => r.EnvironmentId == "Environment-1" && r.ReleaseId == "Release-2");
        toKeep.Should().Contain(r => r.EnvironmentId == "Environment-1" && r.ReleaseId == "Release-3");
        toKeep.Should().Contain(r => r.EnvironmentId == "Environment-2" && r.ReleaseId == "Release-1");
        toKeep.Should().Contain(r => r.EnvironmentId == "Environment-2" && r.ReleaseId == "Release-2");
        toKeep.Should().Contain(r => r.EnvironmentId == "Environment-3" && r.ReleaseId == "Release-1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetReleasesToKeepAsync_InvalidKeepCount_ThrowsArgumentException(int invalidKeepCount)
    {
        // Arrange
        _dataFactory.ConfigureMockData(
            projects: new[] { new Project("Project-1", "Project-1") },
            environments: new[] { new Lib.Models.Environment("Environment-1", "Environment-1") },
            releases: new[] { new Release("Release-1", "Project-1", DateTime.UtcNow, "1.0.0") },
            deployments: new[] { new Deployment("Deploy-1", "Release-1", "Environment-1", DateTime.UtcNow) });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _svc.GetReleasesToKeepAsync(invalidKeepCount));
        ex.ParamName.Should().Be("keepCount");
        ex.Message.Should().Contain("keepCount must be a positive number");
    }

    // Unique edge cases for retention logic
    [Fact]
    public async Task GetReleasesToKeepAsync_TieBreakingByCreationDate_WorksCorrectly()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _dataFactory.ConfigureMockData(
            projects: new[] { new Project("Project-1", "Project-1") },
            environments: new[] { new Lib.Models.Environment("Environment-1", "Environment-1") },
            releases: new[]
            {
                new Release("Release-1", "Project-1", baseTime.AddHours(-2), "1.0.0"),
                new Release("Release-2", "Project-1", baseTime.AddHours(-1), "1.0.1") // More recent creation
            },
            deployments: new[]
            {
                new Deployment("Deploy-1", "Release-1", "Environment-1", baseTime),
                new Deployment("Deploy-2", "Release-2", "Environment-1", baseTime) // Same deployment time
            });

        // Act
        var result = await _svc.GetReleasesToKeepAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainSingle(r => r.ReleaseId == "Release-2"); // Should prefer more recently created release
    }

    [Fact]
    public async Task GetReleasesToKeepAsync_DuplicateIds_UsesFirstOccurrence()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _dataFactory.ConfigureMockData(
            projects: new[]
            {
                new Project("Project-1", "First Project"),
                new Project("Project-1", "Duplicate Project")
            },
            environments: new[]
            {
                new Lib.Models.Environment("Environment-1", "First Environment"),
                new Lib.Models.Environment("Environment-1", "Duplicate Environment")
            },
            releases: new[]
            {
                new Release("Release-1", "Project-1", baseTime.AddHours(-2), "1.0.0"),
                new Release("Release-1", "Project-1", baseTime.AddHours(-1), "1.0.1")
            },
            deployments: new[]
            {
                new Deployment("Deploy-1", "Release-1", "Environment-1", baseTime)
            });

        // Act
        var result = await _svc.GetReleasesToKeepAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainSingle(r => 
            r.ReleaseId == "Release-1" && 
            r.Version == "1.0.0" && // Should use first occurrence
            r.ProjectName == "First Project" &&
            r.EnvironmentName == "First Environment");
    }

    [Fact]
    public async Task GetReleasesToKeepAsync_ComplexScenario_HandlesAllEdgeCases()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _dataFactory.ConfigureMockData(
            projects: new[]
            {
                new Project("Project-1", "Project One"),
                new Project("Project-2", "Project Two")
            },
            environments: new[]
            {
                new Lib.Models.Environment("Environment-1", "Environment One"),
                new Lib.Models.Environment("Environment-2", "Environment Two")
            },
            releases: new[]
            {
                // Valid releases
                new Release("Release-1", "Project-1", baseTime.AddDays(-5), "1.0.0"),
                new Release("Release-2", "Project-1", baseTime.AddDays(-4), "1.0.1"),
                new Release("Release-3", "Project-2", baseTime.AddDays(-3), "2.0.0"),
                
                // Invalid releases
                new Release("Release-4", "NonExistentProject", baseTime.AddDays(-2), "1.0.0"),
                new Release("Release-5", "Project-1", baseTime.AddDays(-1), null!),
                new Release("", "Project-1", baseTime, "1.0.2"), // Empty ID
                new Release("Release-6", "", baseTime, "1.0.3"), // Empty ProjectId
                new Release("Release-7", "Project-1", default, "1.0.4") // Default Created date
            },
            deployments: new[]
            {
                // Valid deployments
                new Deployment("Deploy-1", "Release-1", "Environment-1", baseTime.AddHours(-10)),
                new Deployment("Deploy-2", "Release-2", "Environment-1", baseTime.AddHours(-5)),
                new Deployment("Deploy-3", "Release-1", "Environment-2", baseTime.AddHours(-8)),
                new Deployment("Deploy-4", "Release-3", "Environment-1", baseTime.AddHours(-2)),
                
                // Invalid deployments
                new Deployment("Deploy-5", "NonExistentRelease", "Environment-1", baseTime.AddHours(-1)),
                new Deployment("Deploy-6", "Release-1", "NonExistentEnvironment", baseTime.AddHours(-1)),
                new Deployment("", "Release-1", "Environment-1", baseTime), // Empty ID
                new Deployment("Deploy-7", "", "Environment-1", baseTime), // Empty ReleaseId
                new Deployment("Deploy-8", "Release-1", "", baseTime), // Empty EnvironmentId
                new Deployment("Deploy-9", "Release-1", "Environment-1", default) // Default DeployedAt
            });

        // Act
        var result = await _svc.GetReleasesToKeepAsync(2);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(4); // Should have 4 valid retention results
        
        // Verify correct releases are kept for each project/environment combination
        var project1Staging = resultList.Where(r => r.ProjectId == "Project-1" && r.EnvironmentId == "Environment-1").ToList();
        var project1Production = resultList.Where(r => r.ProjectId == "Project-1" && r.EnvironmentId == "Environment-2").ToList();
        var project2Staging = resultList.Where(r => r.ProjectId == "Project-2" && r.EnvironmentId == "Environment-1").ToList();
        
        project1Staging.Should().HaveCount(2); // Release-1 and Release-2
        project1Production.Should().HaveCount(1); // Release-1 only
        project2Staging.Should().HaveCount(1); // Release-3 only
        
        // Verify order (most recent deployment first)
        project1Staging[0].ReleaseId.Should().Be("Release-2");
        project1Staging[1].ReleaseId.Should().Be("Release-1");
    }

    // Helper classes
    private class MockDataProviderFactory : IDataProviderFactory
    {
        private MockDataProvider _mockProvider;

        public MockDataProviderFactory()
        {
            _mockProvider = new MockDataProvider();
        }

        public void ConfigureMockData(
            IEnumerable<Project>? projects = null,
            IEnumerable<Lib.Models.Environment>? environments = null,
            IEnumerable<Release>? releases = null,
            IEnumerable<Deployment>? deployments = null)
        {
            _mockProvider = new MockDataProvider(projects, environments, releases, deployments);
        }

        public Task<IDataProvider> GetDataProviderAsync()
        {
            return Task.FromResult<IDataProvider>(_mockProvider);
        }
    }

    private class MockDataProvider : IDataProvider
    {
        private readonly IEnumerable<Project>? _projects;
        private readonly IEnumerable<Lib.Models.Environment>? _environments;
        private readonly IEnumerable<Release>? _releases;
        private readonly IEnumerable<Deployment>? _deployments;

        public MockDataProvider(
            IEnumerable<Project>? projects = null,
            IEnumerable<Lib.Models.Environment>? environments = null,
            IEnumerable<Release>? releases = null,
            IEnumerable<Deployment>? deployments = null)
        {
            _projects = projects;
            _environments = environments;
            _releases = releases;
            _deployments = deployments;
        }

        public Task<IEnumerable<Project>> GetProjectsAsync()
        {
            return Task.FromResult(_projects ?? Enumerable.Empty<Project>());
        }

        public Task<IEnumerable<Lib.Models.Environment>> GetEnvironmentsAsync()
        {
            return Task.FromResult(_environments ?? Enumerable.Empty<Lib.Models.Environment>());
        }

        public Task<IEnumerable<Release>> GetReleasesAsync()
        {
            return Task.FromResult(_releases ?? Enumerable.Empty<Release>());
        }

        public Task<IEnumerable<Deployment>> GetDeploymentsAsync()
        {
            return Task.FromResult(_deployments ?? Enumerable.Empty<Deployment>());
        }
    }
}
