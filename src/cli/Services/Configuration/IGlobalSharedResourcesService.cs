using System;

namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Service for managing global shared resources configuration.
/// </summary>
public interface IGlobalSharedResourcesService
{
    /// <summary>
    /// Gets the global shared resources for the specified level and repository slugs.
    /// </summary>
    /// <param name="level">The level at which to retrieve the resources.</param>
    /// <param name="repositorySlugs">The slugs of the repositories.</param>
    /// <returns>The global shared resources.</returns>
    GlobalSharedResources Get(Level level, IEnumerable<string> repositorySlugs);

    /// <summary>
    /// Saves the global shared resources at the specified level and repository slugs.
    /// </summary>
    /// <param name="resources">The resources to save.</param>
    /// <param name="level">The level at which to save the resources.</param>
    /// <param name="repositorySlugs">The slugs of the repositories.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task Save(GlobalSharedResources resources, Level level, IEnumerable<string> repositorySlugs, CancellationToken cancellationToken = default);
}
