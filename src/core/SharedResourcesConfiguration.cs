namespace Spire;

/// <summary>
/// Root configuration model for a shared-resources.json file.
/// </summary>
public sealed record SharedResourcesConfiguration
{
    /// <summary>
    /// A map of resource ID to resource definition.
    /// </summary>
    public required Dictionary<string, SharedResource> Resources { get; init; }

    /// <summary>
    /// External Git repositories whose shared resources are imported.
    /// Only valid in repository-scoped configuration files.
    /// </summary>
    public List<ExternalResource>? ExternalResources { get; init; }
}
