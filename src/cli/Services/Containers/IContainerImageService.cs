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

    /// <summary>
    /// Checks whether a specific tag exists for an image in a registry.
    /// </summary>
    /// <param name="registry">The container registry.</param>
    /// <param name="imageName">The image name.</param>
    /// <param name="tag">The tag to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the tag exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> TagExistsAsync(string registry, string imageName, string tag, CancellationToken cancellationToken);

    /// <summary>
    /// Tags an existing image with additional tags.
    /// </summary>
    /// <param name="sourceImage">The full source image reference (e.g., "registry/name:tag").</param>
    /// <param name="tags">The tags to apply to the image.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task TagImageAsync(string sourceImage, IEnumerable<string> tags, CancellationToken cancellationToken);
}