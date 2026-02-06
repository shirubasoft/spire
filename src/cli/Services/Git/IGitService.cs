namespace Spire.Cli.Services.Git;

/// <summary>
/// Provides operations for managing Git repositories.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Clones a Git repository to the specified destination path.
    /// </summary>
    /// <param name="repositoryUrl">The URL of the Git repository to clone.</param>
    /// <param name="path">The local path where the repository should be cloned.</param>
    /// <param name="branch">The branch to clone. When null, the repository default branch is used.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<GitRepository> CloneRepositoryAsync(string repositoryUrl, string path, string? branch = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves information about a Git repository.
    /// </summary>
    /// <param name="path">The local path of the Git repository.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<GitRepository> GetRepositoryAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a Git repository is already cloned at the specified path.
    /// </summary>
    /// <param name="path">The local path of the Git repository.</param>
    bool IsRepositoryCloned(string path);

    /// <summary>
    /// Gets the root directory of the Git repository containing the specified path.
    /// </summary>
    /// <param name="path">A path within the Git repository.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The repository root path, or null if not in a Git repository.</returns>
    Task<string?> GetRepositoryRootAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the parent directory of the specified repository root.
    /// Used to determine where to clone external repositories.
    /// </summary>
    /// <param name="repoRoot">The repository root path.</param>
    /// <returns>The parent directory path.</returns>
    string GetParentDirectory(string repoRoot);

    /// <summary>
    /// Gets the remote URL for the specified repository.
    /// </summary>
    /// <param name="path">The local path of the Git repository.</param>
    /// <param name="remoteName">The name of the remote (default: "origin").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The remote URL, or null if not found.</returns>
    Task<string?> GetRemoteUrlAsync(string path, string remoteName = "origin", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default branch name for the repository.
    /// </summary>
    /// <param name="path">The local path of the Git repository.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The default branch name.</returns>
    Task<string> GetDefaultBranchAsync(string path, CancellationToken cancellationToken = default);
}
