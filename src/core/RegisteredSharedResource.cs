namespace Spire;

/// <summary>
/// A shared resource registered by a user with local overrides.
/// </summary>
public sealed record RegisteredSharedResource
{
    /// <summary>
    /// The base directory where the resource is stored locally.
    /// </summary>
    public required string BaseDirectory { get; init; }

    /// <summary>
    /// The shared resource definition.
    /// </summary>
    public required SharedResource Resource { get; init; }

    /// <summary>
    /// User-specified overrides for this resource.
    /// </summary>
    public required ResourceOverrides Overrides { get; init; }
}
