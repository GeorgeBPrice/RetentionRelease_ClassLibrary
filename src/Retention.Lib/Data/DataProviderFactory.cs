using Microsoft.Extensions.Logging;
using Retention.Lib.Clients;
using Retention.Lib.Interfaces;
using Retention.Lib.Models.Configuration;

namespace Retention.Lib.Data;


/// <summary>
/// Factory for creating <see cref="IDataProvider"/> instances based on configuration.
/// <para>
/// If <see cref="RetentionOptions.UseLocalData"/> is true, returns a provider that reads data from local JSON files.
/// If false, returns a provider that communicates with the DevOps Deploy API using HTTP.
/// </para>
/// Validates configuration and ensures required dependencies are present for the selected provider.
/// </summary>
public class DataProviderFactory : IDataProviderFactory
{
    private readonly RetentionOptions _options;
    private readonly ILogger<DataProviderFactory> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public DataProviderFactory(
        RetentionOptions options,
        ILogger<DataProviderFactory> logger,
        ILoggerFactory loggerFactory,
        IHttpClientFactory? httpClientFactory = null)
    {
        _options = options;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    public Task<IDataProvider> GetDataProviderAsync()
    {
        _options.Validate();

        if (_options.UseLocalData)
        {
            if (_options.DataFiles == null)
            {
                throw new InvalidOperationException("DataFiles configuration is required when UseLocalData is true");
            }

            _logger.LogInformation("Creating local file data provider");
            return Task.FromResult<IDataProvider>(new JsonFileDataProvider(_options.DataFiles));
        }
        else
        {
            if (_options.DevOpsDeploy == null)
            {
                throw new InvalidOperationException("DevOpsDeploy configuration is required when UseLocalData is false");
            }

            if (string.IsNullOrEmpty(_options.DevOpsDeploy.ApiBaseUrl))
            {
                throw new InvalidOperationException("DevOpsDeploy.ApiBaseUrl is required when UseLocalData is false");
            }

            _logger.LogInformation("Creating DevOps API data provider");
            if (_httpClientFactory == null)
            {
                throw new InvalidOperationException(
                    "IHttpClientFactory is required when using DevOps API. Register it with services.AddHttpClient()");
            }

            var client = _httpClientFactory.CreateClient("DevOpsDeploy");

            // Validate and set the base address
            if (!Uri.TryCreate(_options.DevOpsDeploy.ApiBaseUrl, UriKind.Absolute, out var baseAddress))
            {
                throw new InvalidOperationException($"Invalid API base URL: {_options.DevOpsDeploy.ApiBaseUrl}");
            }
            client.BaseAddress = baseAddress;

            client.Timeout = TimeSpan.FromSeconds(_options.DevOpsDeploy.TimeoutSeconds);

            // Add API key-header and API key
            if (!string.IsNullOrEmpty(_options.DevOpsDeploy.ApiKeyHeader) &&
                !string.IsNullOrEmpty(_options.DevOpsDeploy.ApiKey))
            {
                client.DefaultRequestHeaders.Add(_options.DevOpsDeploy.ApiKeyHeader, _options.DevOpsDeploy.ApiKey);
            }

            // Create client with logger
            var devOpsLogger = _loggerFactory.CreateLogger<DevOpsDeployClient>();
            return Task.FromResult<IDataProvider>(new DevOpsDeployClient(client, _options.DevOpsDeploy, devOpsLogger));
        }
    }
}
