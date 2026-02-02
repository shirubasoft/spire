namespace Spire;

/// <summary>
/// Git repository settings for a shared resource.
/// </summary>
public sealed record GitRepositorySettings
{
    /// <summary>
    /// The repository URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// The default branch name.
    /// </summary>
    public required string DefaultBranch { get; init; } = "main";
}
