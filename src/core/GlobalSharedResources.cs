using System.Diagnostics.CodeAnalysis;

namespace Spire;

/// <summary>
/// Global shared resources configuration (~/.aspire/spire/aspire-shared-resources.json).
/// Contains only resolved resources with absolute paths. No external resource references.
/// </summary>
public sealed record GlobalSharedResources
{
    /// <summary>
    /// An empty instance with no resources.
    /// </summary>
    public static readonly GlobalSharedResources Empty = new() { Resources = [] };

    /// <summary>
    /// A map of resource ID to resource definition.
    /// </summary>
    public required Dictionary<string, SharedResource> Resources { get; init; }

    /// <summary>
    /// Attempts to get a resource by its ID.
    /// </summary>
    /// <param name="id">The resource ID to look up.</param>
    /// <param name="resource">The resource if found; otherwise, null.</param>
    /// <returns>True if the resource was found; otherwise, false.</returns>
    public bool TryGetResource(string id, [NotNullWhen(true)] out SharedResource? resource)
    {
        return Resources.TryGetValue(id, out resource);
    }

    /// <summary>
    /// Gets a resource by its ID, or null if not found.
    /// </summary>
    /// <param name="id">The resource ID to look up.</param>
    /// <returns>The resource if found; otherwise, null.</returns>
    public SharedResource? GetResource(string id)
    {
        return Resources.GetValueOrDefault(id);
    }
}
