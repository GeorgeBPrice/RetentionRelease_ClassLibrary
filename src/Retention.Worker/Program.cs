using Microsoft.Extensions.Logging.Console;
using Retention.Lib.Models.Configuration;
using Retention.Lib.Interfaces;
using Retention.Lib.Data;
using Retention.Lib.Services;
using Retention.Worker;
using Retention.Worker.Console;

/// <summary>
/// Entry point for the Retention Worker Service.
/// 
/// This program sets up and runs a .NET Worker Service that manages release retention logic.
/// 
/// Key components and flow:
/// - <b>Host Builder</b>: Uses <see cref="Host.CreateApplicationBuilder"/> to configure the application host, including configuration, logging, and dependency injection (DI) container.
/// - <b>Configuration</b>: Reads settings (such as UseLocalData and KeepCount) from configuration sources (appsettings, environment, etc.) and binds them to a <see cref="RetentionOptions"/> instance.
/// - <b>Dependency Injection (DI) Container</b>: Registers services and options as singletons or hosted services, making them available for injection throughout the application.
///   - Registers <see cref="IDataProviderFactory"/>, <see cref="IReleaseRetentionService"/>, and the <see cref="RetentionWorker"/> background service.
///   - Adds <see cref="HttpClient"/> for API-based data access.
/// - <b>Logging</b>: Configures console logging with a custom formatter.
/// - <b>Startup Validation</b>: If running in local data mode, verifies that all required data files exist before starting the worker.
/// - <b>Host Execution</b>: Builds and runs the host, which starts the background worker service. Handles startup and unexpected termination with appropriate logging.
/// 
/// The main purpose of this setup is to provide a robust, configurable, and testable background service for managing release retention, leveraging .NET's built-in hosting, DI, and logging infrastructure.
/// </summary>
var builder = Host.CreateApplicationBuilder(args);

// Create and bind configuration
var options = new RetentionOptions
{
    UseLocalData = builder.Configuration.GetValue<bool>("UseLocalData"),
    KeepCount = builder.Configuration.GetValue<int>("KeepCount")
};
builder.Configuration.Bind(options);

// Register services
builder.Services.AddHttpClient();  // Required for API mode
builder.Services.AddSingleton(options);
builder.Services.AddSingleton<IDataProviderFactory, DataProviderFactory>();
builder.Services.AddSingleton<IReleaseRetentionService, ReleaseRetentionService>();
builder.Services.AddHostedService<RetentionWorker>();

// Configure logging with a cleaner format and filter out hosting logs
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole(options =>
    {
        options.FormatterName = "clean";
    });
    logging.AddConsoleFormatter<CleanConsoleFormatter, ConsoleFormatterOptions>();
    logging.SetMinimumLevel(LogLevel.Information);
    // Filter out Microsoft.Hosting.Lifetime logs
    logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
});

// Add the custom formatter
builder.Services.Configure<ConsoleFormatterOptions>(options =>
{
    options.IncludeScopes = false;
});

// Create the host but don't start it yet
var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("=== Retention Worker Starting ===");
logger.LogInformation("UseLocalData: {UseLocalData}", options.UseLocalData);
logger.LogInformation("Data Source: {DataSource}", options.UseLocalData ? "Local Files" : "DevOps Deploy API");
logger.LogInformation("Keep Count: {KeepCount}", options.KeepCount);

if (options.UseLocalData)
{
    // Verify that the data files exist
    var dataFiles = new[]
    {
        options.DataFiles!.Projects,
        options.DataFiles.Environments,
        options.DataFiles.Releases,
        options.DataFiles.Deployments
    };

    var missingFiles = dataFiles.Where(file => !File.Exists(file)).ToArray();
    if (missingFiles.Any())
    {
        logger.LogError("Missing required data files:");
        foreach (var file in missingFiles)
        {
            logger.LogError("  - {File}", file);
        }
        logger.LogError("Please ensure all data files are present before running the worker.");
        Environment.Exit(1);
        return;
    }

    logger.LogInformation("All required data files found.");
}

logger.LogInformation("===============================");

try
{
    // Start the host which will start the worker
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogError(ex, "Application terminated unexpectedly");
    Environment.Exit(1);
}