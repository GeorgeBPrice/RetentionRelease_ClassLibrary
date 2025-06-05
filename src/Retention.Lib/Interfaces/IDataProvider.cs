using Retention.Lib.Models;

namespace Retention.Lib.Interfaces;


/// <summary>
/// Defines methods for asynchronously retrieving project, environment, release, and deployment data.
/// Implementations of this interface should provide access to the underlying data source,
/// returning collections of domain models as asynchronous operations.
/// </summary>
public interface IDataProvider
{
    Task<IEnumerable<Project>> GetProjectsAsync();
    Task<IEnumerable<Models.Environment>> GetEnvironmentsAsync();
    Task<IEnumerable<Release>> GetReleasesAsync();
    Task<IEnumerable<Deployment>> GetDeploymentsAsync();
}
