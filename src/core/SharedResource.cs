namespace Spire;

/// <summary>
/// Defines a shared resource that can be run in container or project mode.
/// </summary>
public record SharedResource
{
    /// <summary>
    /// The default execution mode.
    /// </summary>
    public required Mode DefaultMode { get; init; }

    /// <summary>
    /// Container mode settings, if supported.
    /// </summary>
    public virtual required ContainerModeSettings? ContainerMode { get; init; }

    /// <summary>
    /// Project mode settings, if supported.
    /// </summary>
    public virtual required ProjectModeSettings? ProjectMode { get; init; }

    /// <summary>
    /// Git repository settings, if applicable.
    /// </summary>
    public virtual required GitRepositorySettings? GitRepository { get; init; }
}
