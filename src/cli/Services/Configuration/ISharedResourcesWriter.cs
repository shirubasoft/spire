namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Writes shared resources configuration to disk.
/// </summary>
public interface ISharedResourcesWriter
{
    /// <summary>
    /// Saves resources to the global config file. When <paramref name="repoPath"/> is provided,
    /// saves to the repository-scoped override file instead (slug is derived from the repo's git remote).
    /// </summary>
    Task SaveGlobalAsync(GlobalSharedResources resources, string? repoPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves repository shared resources to the .aspire/settings.json file at the given path.
    /// </summary>
    Task SaveRepositoryAsync(RepositorySharedResources resources, string repoPath, CancellationToken cancellationToken = default);
}
