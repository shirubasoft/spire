namespace Spire;

/// <summary>
/// User-specified overrides for a shared resource.
/// </summary>
public sealed record ResourceOverrides
{
    /// <summary>
    /// Overrides the default execution mode.
    /// </summary>
    public Mode? Mode { get; init; }
}
