namespace Spire.Cli.Services;

/// <summary>
/// Represents a container image reference with its registry, name, and tag.
/// </summary>
public readonly record struct ContainerImage
{
    /// <summary>
    /// Gets the name of the container image.
    /// </summary>
    public required string ImageName { get; init; }

    /// <summary>
    /// Gets the tag of the container image.
    /// </summary>
    public required string ImageTag { get; init; }

    /// <summary>
    /// Gets the registry where the container image is hosted.
    /// </summary>
    public required string ImageRegistry { get; init; }
}
