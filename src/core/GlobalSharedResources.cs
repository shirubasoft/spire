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
    /// Creates a new <see cref="GlobalSharedResources"/> with the specified resource updated.
    /// </summary>
    /// <param name="id">The resource ID to update.</param>
    /// <param name="resource">The updated resource.</param>
    /// <returns>A new instance with the updated resource.</returns>
    public GlobalSharedResources UpdateResource(string id, SharedResource resource)
    {
        var newResources = new Dictionary<string, SharedResource>(Resources)
        {
            [id] = resource
        };

        return this with { Resources = newResources };
    }
}
