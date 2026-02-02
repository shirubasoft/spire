using System;

namespace Spire.Cli.Services;

/// <summary>
/// Provides operations for managing container images.
/// </summary>
public interface IContainerImageService
{
    /// <summary>
    /// Checks whether a container image exists.
    /// </summary>
    /// <param name="image">The container image to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the image exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> ImageExistsAsync(ContainerImage image, CancellationToken cancellationToken);

    /// <summary>
    /// Builds a container image from the specified request.
    /// </summary>
    /// <param name="request">The build request containing image details and build configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task BuildImageAsync(ContainerImageBuildRequest request, CancellationToken cancellationToken);
}
