namespace Spire;

/// <summary>
/// Git repository settings for a shared resource.
/// </summary>
public sealed record GitRepositorySettings
{
    /// <summary>
    /// The repository URL.
    /// </summary>
    public required Uri Url { get; init; }

    /// <summary>
    /// The default branch name.
    /// </summary>
    public required string DefaultBranch { get; init; } = "main";

    /// <summary>
    /// Gets the repository slug derived from the URL.
    /// </summary>
    /// <returns></returns>
    public string GetRepositorySlug()
    {
        return Url.AbsolutePath.TrimStart('/').Replace('/', '-').Replace(".git", "");
    }
}
