namespace Spire;

/// <summary>
/// Repository-scoped shared resources configuration.
/// Contains resource definitions with relative paths and optional external repository imports.
/// </summary>
public sealed record RepositorySharedResources
{
    /// <summary>
    /// A map of resource ID to resource definition.
    /// </summary>
    public required Dictionary<string, SharedResource> Resources { get; init; }

    /// <summary>
    /// External Git repositories whose shared resources are imported.
    /// </summary>
    public List<ExternalResource>? ExternalResources { get; init; }
}
