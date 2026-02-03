namespace Spire.Cli.Services.Git;

/// <summary>
/// Represents a Git repository with its current state.
/// </summary>
public readonly record struct GitRepository
{
    /// <summary>
    /// The root path of the Git repository.
    /// </summary>
    public required string RootPath { get; init; }

    /// <summary>
    /// The current branch of the Git repository.
    /// </summary>
    public required string CurrentBranch { get; init; }

    /// <summary>
    /// The latest commit hash of the Git repository.
    /// </summary>
    public required string LatestCommitHash { get; init; }

    /// <summary>
    /// Indicates whether the Git repository has uncommitted changes.
    /// </summary>
    public required bool IsDirty { get; init; }

    /// <summary>
    /// The remote URL of the repository (origin).
    /// </summary>
    public string? RemoteUrl { get; init; }
}