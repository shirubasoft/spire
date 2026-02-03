using Spire.Cli.Services.Git;

namespace Spire.Cli.Services;

/// <summary>
/// Generates image tags based on Git repository state.
/// </summary>
public interface IImageTagGenerator
{
    /// <summary>
    /// Generates image tags for a container build.
    /// Returns three tags: commit hash (7 chars), safe branch name, and "latest".
    /// </summary>
    /// <param name="repository">The Git repository information.</param>
    /// <returns>An enumerable of image tags.</returns>
    ImageTags Generate(GitRepository repository);
}

/// <summary>
/// Represents the set of image tags generated for a build.
/// </summary>
public sealed record ImageTags
{
    /// <summary>
    /// The short commit hash tag (7 characters).
    /// </summary>
    public required string CommitTag { get; init; }

    /// <summary>
    /// The sanitized branch name tag.
    /// </summary>
    public required string BranchTag { get; init; }

    /// <summary>
    /// The "latest" tag.
    /// </summary>
    public string LatestTag { get; } = "latest";

    /// <summary>
    /// Gets all tags as an enumerable.
    /// </summary>
    public IEnumerable<string> All => [CommitTag, BranchTag, LatestTag];
}