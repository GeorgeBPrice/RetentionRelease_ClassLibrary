using Retention.Lib.Models;

namespace Retention.Lib.Interfaces
{
    public interface IReleaseRetentionService
    {
        /// <summary>
        /// For each project/environment, keep the N releases most recently deployed.
        /// The service will fetch the required data from either local JSON files or the DevOps Deploy API.
        /// </summary>
        /// <param name="keepCount">How many releases to keep per project/env.</param>
        /// <returns>The set of releases to keep.</returns>
        Task<IReadOnlyCollection<RetentionResult>> GetReleasesToKeepAsync(int keepCount);
    }
}
