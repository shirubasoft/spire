using Spire.Cli.Services.Git;

namespace Spire.Cli.Services.Analysis;

/// <summary>
/// Detects Git repository settings for resource generation.
/// </summary>
public sealed class GitSettingsDetector : IGitSettingsDetector
{
    private readonly IGitService _gitService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitSettingsDetector"/> class.
    /// </summary>
    /// <param name="gitService">The Git service.</param>
    public GitSettingsDetector(IGitService gitService)
    {
        _gitService = gitService;
    }

    /// <inheritdoc />
    public async Task<GitSettingsResult?> DetectAsync(string path, CancellationToken cancellationToken = default)
    {
        var repoRoot = await _gitService.GetRepositoryRootAsync(path, cancellationToken);
        if (repoRoot is null)
        {
            return null;
        }

        var remoteUrl = await _gitService.GetRemoteUrlAsync(repoRoot, "origin", cancellationToken);
        if (remoteUrl is null)
        {
            return null;
        }

        var normalizedUrl = NormalizeGitUrl(remoteUrl);
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var defaultBranch = await _gitService.GetDefaultBranchAsync(repoRoot, cancellationToken);

        return new GitSettingsResult
        {
            RepositoryRoot = repoRoot,
            RemoteUrl = uri,
            DefaultBranch = defaultBranch
        };
    }

    private static string NormalizeGitUrl(string url)
    {
        // Convert SSH URLs to HTTPS URLs
        // git@github.com:user/repo.git -> https://github.com/user/repo
        if (url.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
        {
            var sshUrlPart = url["git@".Length..];
            var colonIndex = sshUrlPart.IndexOf(':');
            if (colonIndex > 0)
            {
                var host = sshUrlPart[..colonIndex];
                var path = sshUrlPart[(colonIndex + 1)..];

                // Remove .git suffix if present
                if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                {
                    path = path[..^4];
                }

                return $"https://{host}/{path}";
            }
        }

        // Remove .git suffix from HTTPS URLs
        if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            return url[..^4];
        }

        return url;
    }
}
