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
}
