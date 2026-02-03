namespace Spire.Cli.Services;

/// <summary>
/// Represents a request to build a container image.
/// </summary>
public sealed record ContainerImageBuildRequest
{
    /// <summary>
    /// Gets the container image to build.
    /// </summary>
    public required ContainerImage Image { get; init; }

    /// <summary>
    /// Gets the build command to execute.
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Gets the working directory for the build process.
    /// </summary>
    public required string WorkingDirectory { get; init; }

    /// <summary>
    /// Gets additional tags to apply to the built image.
    /// </summary>
    public IEnumerable<string> AdditionalTags { get; init; } = [];
}