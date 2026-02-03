using Spire.Cli.Services;

namespace Spire.Cli.Tests.TestHelpers;

/// <summary>
/// A test implementation of IGitService.
/// </summary>
public sealed class TestGitService : IGitService
{
    private readonly Dictionary<string, GitRepository> _repositories = new();

    /// <summary>
    /// Registers a test repository at the given path.
    /// </summary>
    public void RegisterRepository(string path, GitRepository repository)
    {
        _repositories[Path.GetFullPath(path)] = repository;
    }

    /// <inheritdoc/>
    public Task<GitRepository> CloneRepositoryAsync(string repositoryUrl, string path, CancellationToken cancellationToken)
    {
        var repo = new GitRepository
        {
            RootPath = path,
            CurrentBranch = "main",
            LatestCommitHash = "abc123",
            IsDirty = false
        };
        _repositories[Path.GetFullPath(path)] = repo;
        return Task.FromResult(repo);
    }

    /// <inheritdoc/>
    public Task<GitRepository> GetRepositoryAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);

        // Walk up the directory tree to find the repository root
        var current = fullPath;
        while (current is not null)
        {
            if (_repositories.TryGetValue(current, out var repo))
            {
                return Task.FromResult(repo);
            }
            current = Path.GetDirectoryName(current);
        }

        throw new InvalidOperationException($"No git repository found at '{path}'");
    }

    /// <inheritdoc/>
    public bool IsRepositoryCloned(string path)
    {
        var fullPath = Path.GetFullPath(path);

        // Walk up the directory tree to find the repository root
        var current = fullPath;
        while (current is not null)
        {
            if (_repositories.ContainsKey(current))
            {
                return true;
            }
            current = Path.GetDirectoryName(current);
        }

        return false;
    }
}
