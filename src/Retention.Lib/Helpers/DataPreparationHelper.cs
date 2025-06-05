using Microsoft.Extensions.Logging;

namespace Retention.Lib.Helpers;


/// <summary>
/// Provides helper methods for preparing and validating data collections,
/// such as creating lookup dictionaries by ID and ensuring required collections are populated.
/// </summary>
public static class DataPreparationHelper
{
    /// <summary>
    /// Creates a dictionary lookup from a collection of items using a specified ID selector.
    /// Logs any duplicate IDs found in the collection.
    /// </summary>
    public static Dictionary<string, T> CreateLookupById<T>(
        IEnumerable<T> items,
        Func<T, string> idSelector,
        string itemType,
        ILogger logger) where T : class
    {
        var lookup = items
            .Where(i => i != null && !string.IsNullOrWhiteSpace(idSelector(i)))
            .GroupBy(idSelector)
            .ToDictionary(g => g.Key, g => g.First());

        LoggingHelper.LogDuplicates(items, itemType, idSelector, logger);

        return lookup;
    }

    /// <summary>
    /// Validates that the required collections (projects, environments, releases, deployments) are not empty.
    /// </summary>
    public static void ValidateRequiredCollections(
        IEnumerable<object> projects,
        IEnumerable<object> environments,
        IEnumerable<object> releases,
        IEnumerable<object> deployments)
    {
        var requiredCollections = new (IEnumerable<object> Collection, string Name)[]
        {
            (projects, "projects"),
            (environments, "environments"),
            (releases, "releases"),
            (deployments, "deployments")
        };

        foreach (var (collection, name) in requiredCollections)
        {
            if (!collection.Any())
                throw new InvalidOperationException($"No {name} found.");
        }
    }
}
