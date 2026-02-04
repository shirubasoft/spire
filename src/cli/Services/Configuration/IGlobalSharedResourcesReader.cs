namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Reads shared resources configuration from the global config file,
/// resolving image tags from the current Git branch state.
/// </summary>
public interface IGlobalSharedResourcesReader
{
    /// <summary>
    /// Loads shared resource configuration from the global config file and resolves
    /// each resource's <see cref="ContainerModeSettings.ImageTag"/> to the current
    /// Git branch tag using <see cref="IImageTagGenerator"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The global shared resources with resolved image tags.</returns>
    Task<GlobalSharedResources> GetSharedResourcesAsync(CancellationToken cancellationToken = default);
}
