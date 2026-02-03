using CliWrap;
using CliWrap.Buffered;

namespace Spire.Cli.Services.Git;

/// <summary>
/// Resolves the Git CLI executable to use for Git operations.
/// Prefers GitHub CLI (gh) when available, falls back to git.
/// </summary>
public sealed class GitCliResolver : IGitCliResolver
{
    private const string GitHubCli = "gh";
    private const string GitCli = "git";

    /// <inheritdoc />
    public async Task<string> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (await IsCommandAvailableAsync(GitHubCli, cancellationToken))
        {
            return GitHubCli;
        }

        return GitCli;
    }

    private static async Task<bool> IsCommandAvailableAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await CliWrap.Cli.Wrap(command)
                .WithArguments("--version")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken);

            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
