namespace Spire.Cli.Services.Analysis;

/// <summary>
/// Detects Git repository settings for resource generation.
/// </summary>
public interface IGitSettingsDetector
{
    /// <summary>
    /// Detects Git settings for the repository containing the specified path.
    /// </summary>
    /// <param name="path">A path within the Git repository.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The detected settings, or null if not in a Git repository.</returns>
    Task<GitSettingsResult?> DetectAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// The result of detecting Git settings.
/// </summary>
public sealed record GitSettingsResult
{
    /// <summary>
    /// The repository root path.
    /// </summary>
    public required string RepositoryRoot { get; init; }

    /// <summary>
    /// The remote URL (typically origin).
    /// </summary>
    public required Uri RemoteUrl { get; init; }

    /// <summary>
    /// The default branch name.
    /// </summary>
    public required string DefaultBranch { get; init; }
}
