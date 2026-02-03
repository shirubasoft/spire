using System;

namespace Spire.Cli.Services;

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
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<GitRepository> CloneRepositoryAsync(string repositoryUrl, string path, CancellationToken cancellationToken);

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
}