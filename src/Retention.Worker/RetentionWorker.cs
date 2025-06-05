using Retention.Lib.Interfaces;
using Retention.Lib.Models.Configuration;

namespace Retention.Worker;

/// <summary>
/// A .NET Worker Service that demonstrates the implementation and usage of the Retention.Lib class library.
/// 
/// <para>
/// <b>What is a .NET Worker?</b><br/>
/// A .NET Worker is a long-running background service application, typically used for processing tasks, 
/// running scheduled jobs, or handling background operations outside the context of a web application. 
/// Worker Services are commonly hosted as Windows Services or Linux daemons.
/// </para>
/// 
/// <para>
/// <b>How does BackgroundService work?</b><br/>
/// <see cref="BackgroundService"/> is an abstract base class provided by .NET for implementing long-running services. 
/// It manages the service lifetime and provides an <c>ExecuteAsync</c> method where the background processing logic is implemented.
/// The service starts when the host starts and stops gracefully when the host is shutting down or when cancellation is requested.
/// </para>
/// 
/// <para>
/// <b>Purpose of this Worker:</b><br/>
/// This worker is intended solely as a demonstration of how to integrate and use the Retention.Lib class library. 
/// It loads configuration, logs relevant information, and invokes the release retention analysis logic provided by Retention.Lib. 
/// The results are logged, and the application is stopped after execution.
/// </para>
/// </summary>
public class RetentionWorker : BackgroundService
{
    private readonly ILogger<RetentionWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RetentionOptions _config;
    private readonly IHostApplicationLifetime _hostLifetime;

    public RetentionWorker(
        ILogger<RetentionWorker> logger,
        IServiceProvider serviceProvider,
        RetentionOptions config,
        IHostApplicationLifetime hostLifetime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = config;
        _hostLifetime = hostLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Log the start of the worker execution and configuration details
            // NOTE: Retention.Lib will do it's own logging
            _logger.LogInformation("Retention Worker starting execution...");

            if (_config.UseLocalData)
            {
                _logger.LogInformation("Data Files Loaded:");
                _logger.LogInformation("Projects: {Projects}", _config.DataFiles!.Projects);
                _logger.LogInformation("Environments: {Environments}", _config.DataFiles.Environments);
                _logger.LogInformation("Releases: {Releases}", _config.DataFiles.Releases);
                _logger.LogInformation("Deployments: {Deployments}", _config.DataFiles.Deployments);
            }
            else
            {
                _logger.LogInformation("API Configuration =>");
                _logger.LogInformation("API Base URL: {ApiBaseUrl}", _config.DevOpsDeploy!.ApiBaseUrl);
                _logger.LogInformation("API Key Header: {ApiBaseHeader}", _config.DevOpsDeploy!.ApiKeyHeader);
                _logger.LogInformation("Space ID: {SpaceId}", _config.DevOpsDeploy.SpaceId);
            }

            _logger.LogInformation("===============================");
            _logger.LogInformation("Executing Release Retention analysis from Retention.Lib...");

            // Create a scope for the retention service
            using var scope = _serviceProvider.CreateScope();
            var retentionService = scope.ServiceProvider.GetRequiredService<IReleaseRetentionService>();

            // Execute the retention analysis
            var results = await retentionService.GetReleasesToKeepAsync(_config.KeepCount);

            if (results.Any())
            {
                _logger.LogInformation("Releases to keep:");
                foreach (var result in results.OrderBy(r => r.ProjectId).ThenBy(r => r.EnvironmentId))
                {
                    _logger.LogInformation(
                        "  Release: {ReleaseId}, Project: {ProjectId}, Environment: {EnvironmentId}, Version: {Version}, Last Deployed: {LastDeployedAt:yyyy-MM-dd HH:mm:ss}",
                        result.ReleaseId,
                        result.ProjectName,
                        result.EnvironmentName,
                        result.Version,
                        result.LastDeployedAt);
                }
            }
            else
            {
                _logger.LogWarning("No releases found to keep based on the current configuration.");
            }

            // Stop the application after completion
            _logger.LogInformation("Retention Worker execution completed. Stopping application...");
            _hostLifetime.StopApplication();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker is shutting down...");
        }
        // Handle specific exceptions that may occur during execution of the retention service
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument passed to ReleaseRetentionService: {Message}", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid or incomplete data passed to ReleaseRetentionService: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during retention analysis");
            throw;
        }
    }
}