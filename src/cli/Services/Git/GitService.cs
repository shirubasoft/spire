using CliWrap;
using CliWrap.Buffered;

namespace Spire.Cli.Services.Git;

/// <summary>
/// Provides operations for managing Git repositories using CliWrap.
/// </summary>
public sealed class GitService : IGitService
{
    private readonly IGitCliResolver _cliResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitService"/> class.
    /// </summary>
    /// <param name="cliResolver">The Git CLI resolver.</param>
    public GitService(IGitCliResolver cliResolver)
    {
        _cliResolver = cliResolver;
    }

    /// <inheritdoc />
    public async Task<GitRepository> CloneRepositoryAsync(string repositoryUrl, string path, CancellationToken cancellationToken)
    {
        var cli = await _cliResolver.ResolveAsync(cancellationToken);

        var arguments = cli == "gh"
            ? $"repo clone {repositoryUrl} {path}"
            : $"clone {repositoryUrl} {path}";

        await CliWrap.Cli.Wrap(cli)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.ZeroExitCode)
            .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
            .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
            .ExecuteAsync(cancellationToken);

        return await GetRepositoryAsync(path, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GitRepository> GetRepositoryAsync(string path, CancellationToken cancellationToken)
    {
        var rootPath = await GetRepositoryRootAsync(path, cancellationToken)
            ?? throw new InvalidOperationException($"Path '{path}' is not inside a Git repository.");

        var branch = await GetCurrentBranchAsync(rootPath, cancellationToken);
        var commitHash = await GetLatestCommitHashAsync(rootPath, cancellationToken);
        var isDirty = await GetIsDirtyAsync(rootPath, cancellationToken);
        var remoteUrl = await GetRemoteUrlAsync(rootPath, "origin", cancellationToken);

        return new GitRepository
        {
            RootPath = rootPath,
            CurrentBranch = branch,
            LatestCommitHash = commitHash,
            IsDirty = isDirty,
            RemoteUrl = remoteUrl
        };
    }

    /// <inheritdoc />
    public bool IsRepositoryCloned(string path)
    {
        return Directory.Exists(Path.Combine(path, ".git"));
    }

    /// <inheritdoc />
    public async Task<string?> GetRepositoryRootAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var result = await CliWrap.Cli.Wrap("git")
                .WithArguments("rev-parse --show-toplevel")
                .WithWorkingDirectory(path)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken);

            if (result.ExitCode != 0)
            {
                return null;
            }

            return result.StandardOutput.Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string GetParentDirectory(string repoRoot)
    {
        return Path.GetDirectoryName(repoRoot)
            ?? throw new InvalidOperationException($"Cannot get parent directory of '{repoRoot}'.");
    }

    /// <inheritdoc />
    public async Task<string?> GetRemoteUrlAsync(string path, string remoteName = "origin", CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await CliWrap.Cli.Wrap("git")
                .WithArguments($"remote get-url {remoteName}")
                .WithWorkingDirectory(path)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken);

            if (result.ExitCode != 0)
            {
                return null;
            }

            return result.StandardOutput.Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string> GetDefaultBranchAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get the default branch from remote
            var result = await CliWrap.Cli.Wrap("git")
                .WithArguments("symbolic-ref refs/remotes/origin/HEAD --short")
                .WithWorkingDirectory(path)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken);

            if (result.ExitCode == 0)
            {
                var branch = result.StandardOutput.Trim();
                // Remove "origin/" prefix if present
                return branch.StartsWith("origin/", StringComparison.OrdinalIgnoreCase)
                    ? branch["origin/".Length..]
                    : branch;
            }
        }
        catch
        {
            // Fall through to default
        }

        return "main";
    }

    private static async Task<string> GetCurrentBranchAsync(string path, CancellationToken cancellationToken)
    {
        var result = await CliWrap.Cli.Wrap("git")
            .WithArguments("rev-parse --abbrev-ref HEAD")
            .WithWorkingDirectory(path)
            .WithValidation(CommandResultValidation.ZeroExitCode)
            .ExecuteBufferedAsync(cancellationToken);

        return result.StandardOutput.Trim();
    }

    private static async Task<string> GetLatestCommitHashAsync(string path, CancellationToken cancellationToken)
    {
        var result = await CliWrap.Cli.Wrap("git")
            .WithArguments("rev-parse HEAD")
            .WithWorkingDirectory(path)
            .WithValidation(CommandResultValidation.ZeroExitCode)
            .ExecuteBufferedAsync(cancellationToken);

        return result.StandardOutput.Trim();
    }

    private static async Task<bool> GetIsDirtyAsync(string path, CancellationToken cancellationToken)
    {
        var result = await CliWrap.Cli.Wrap("git")
            .WithArguments("status --porcelain")
            .WithWorkingDirectory(path)
            .WithValidation(CommandResultValidation.ZeroExitCode)
            .ExecuteBufferedAsync(cancellationToken);

        return !string.IsNullOrWhiteSpace(result.StandardOutput);
    }
}
