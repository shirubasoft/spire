namespace Spire;

/// <summary>
/// Configuration for running a shared resource in container mode.
/// </summary>
public sealed record ContainerModeSettings
{
    /// <summary>
    /// The container image name.
    /// </summary>
    public required string ImageName { get; init; }

    /// <summary>
    /// The container image registry.
    /// </summary>
    public required string ImageRegistry { get; init; }

    /// <summary>
    /// The container image tag.
    /// </summary>
    public required string ImageTag { get; init; }

    /// <summary>
    /// The command used to build the container image.
    /// </summary>
    public required string BuildCommand { get; init; }

    /// <summary>
    /// The working directory for the build command.
    /// </summary>
    public required string BuildWorkingDirectory { get; init; }
}