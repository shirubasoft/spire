using System;

namespace Spire.Cli.Services.Configuration;

public interface IRepositorySharedResourcesService
{
    /// <summary>
    /// Gets the repository shared resources for the specified path.
    /// </summary>
    /// <param name="path">The path of the repository.</param>
    /// <returns>The repository shared resources, or null if not found.</returns>
    RepositorySharedResources? Get(string path);

    /// <summary>
    /// Saves the repository shared resources for the specified path.
    /// </summary>
    /// <param name="resources">The resources to save.</param>
    /// <param name="path">The path of the repository.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task Save(RepositorySharedResources resources, string path, CancellationToken cancellationToken = default);
}
