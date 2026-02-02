namespace Spire;

/// <summary>
/// An external Git repository whose shared resources are imported.
/// </summary>
public sealed record ExternalResource
{
    /// <summary>
    /// The URL of the external Git repository.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// The branch to use.
    /// </summary>
    public string Branch { get; init; } = "main";
}
