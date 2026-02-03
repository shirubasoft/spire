namespace Spire;

/// <summary>
/// Repository-scoped shared resources configuration.
/// Contains resource definitions with relative paths and optional external repository imports.
/// </summary>
public sealed record RepositorySharedResources
{
    /// <summary>
    /// An empty instance with no resources.
    /// </summary>
    public static readonly RepositorySharedResources Empty = new() { Resources = [] };

    /// <summary>
    /// A map of resource ID to resource definition.
    /// </summary>
    public required Dictionary<string, SharedResource> Resources { get; init; }

    /// <summary>
    /// External Git repositories whose shared resources are imported.
    /// </summary>
    public List<ExternalResource> ExternalResources { get; init; } = [];

    /// <summary>
    /// Gets the number of resources in this instance.
    /// </summary>
    public int Count => Resources.Count;

    /// <summary>
    /// Returns true if a resource with the given ID exists.
    /// </summary>
    /// <param name="id">The resource identifier.</param>
    /// <returns>True if the resource exists; otherwise, false.</returns>
    public bool ContainsResource(string id) => Resources.ContainsKey(id);

    /// <summary>
    /// Returns a new instance without the specified resource.
    /// Does not throw if the resource does not exist.
    /// </summary>
    /// <param name="id">The resource identifier to remove.</param>
    /// <returns>A new instance without the specified resource, or unchanged if not found.</returns>
    public RepositorySharedResources RemoveResource(string id)
    {
        if (!Resources.ContainsKey(id))
        {
            return this;
        }

        var newResources = new Dictionary<string, SharedResource>(Resources);
        newResources.Remove(id);
        return new RepositorySharedResources
        {
            Resources = newResources,
            ExternalResources = ExternalResources
        };
    }

    /// <summary>
    /// Returns a new instance with the specified resources cleared.
    /// If no IDs are provided, clears all resources.
    /// </summary>
    /// <param name="ids">The resource identifiers to clear, or null to clear all.</param>
    /// <returns>A new instance with the specified resources cleared.</returns>
    public RepositorySharedResources ClearResources(IEnumerable<string>? ids = null)
    {
        if (ids is null)
        {
            return new RepositorySharedResources
            {
                Resources = [],
                ExternalResources = ExternalResources
            };
        }

        var newResources = new Dictionary<string, SharedResource>(Resources);
        foreach (var id in ids)
        {
            newResources.Remove(id);
        }

        return new RepositorySharedResources
        {
            Resources = newResources,
            ExternalResources = ExternalResources
        };
    }
}
