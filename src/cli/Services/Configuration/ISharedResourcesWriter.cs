namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Writes shared resources configuration to disk.
/// </summary>
public interface ISharedResourcesWriter
{
    /// <summary>
    /// Saves resources to the global config file at ~/.aspire/spire/aspire-shared-resources.json.
    /// </summary>
    Task SaveGlobalAsync(GlobalSharedResources resources, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves repository shared resources to the .aspire/settings.json file at the given path.
    /// These are meant to be imported into the global configuration.
    /// </summary>
    Task SaveRepositoryAsync(RepositorySharedResources resources, string repoPath, CancellationToken cancellationToken = default);
}
