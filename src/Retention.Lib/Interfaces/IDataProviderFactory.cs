namespace Retention.Lib.Interfaces;

public interface IDataProviderFactory
{
    /// <summary>
    /// Gets the appropriate data provider based on configuration.
    /// If UseLocalData is true, returns a provider that uses local JSON files.
    /// If UseLocalData is false, returns a provider that uses the DevOps API client.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when UseLocalData is false but the DevOps API client is not properly configured,
    /// or when the DevOps API is not accessible.
    /// </exception>
    Task<IDataProvider> GetDataProviderAsync();

} 