namespace Spire.Cli.Services.Git;

/// <summary>
/// Resolves the Git CLI executable to use for Git operations.
/// Prefers GitHub CLI (gh) when available, falls back to git.
/// </summary>
public interface IGitCliResolver
{
    /// <summary>
    /// Resolves the Git CLI executable path.
    /// Returns "gh" if GitHub CLI is available, otherwise "git".
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The path or name of the Git CLI executable.</returns>
    Task<string> ResolveAsync(CancellationToken cancellationToken = default);
}
