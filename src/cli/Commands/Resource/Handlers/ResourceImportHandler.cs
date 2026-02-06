using Spectre.Console;

using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Commands.Resource.Handlers;

/// <summary>
/// Handles the resource import command.
/// </summary>
public sealed class ResourceImportHandler
{
    private readonly IAnsiConsole _console;
    private readonly IGitService _gitService;
    private readonly IRepositorySharedResourcesReader _repositoryReader;
    private readonly ISharedResourcesWriter _writer;
    private readonly IGlobalSharedResourcesReader _globalReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceImportHandler"/> class.
    /// </summary>
    public ResourceImportHandler(
        IAnsiConsole console,
        IGitService gitService,
        IRepositorySharedResourcesReader repositoryReader,
        ISharedResourcesWriter writer,
        IGlobalSharedResourcesReader globalReader)
    {
        _console = console;
        _gitService = gitService;
        _repositoryReader = repositoryReader;
        _writer = writer;
        _globalReader = globalReader;
    }

    /// <summary>
    /// Executes the import command.
    /// </summary>
    /// <param name="yes">Skip confirmation prompts.</param>
    /// <param name="force">Overwrite existing resources.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> ExecuteAsync(bool yes, bool force, CancellationToken cancellationToken = default)
    {
        // Find current git repository root
        var currentDir = Directory.GetCurrentDirectory();
        var repoRoot = await _gitService.GetRepositoryRootAsync(currentDir, cancellationToken);

        if (repoRoot is null)
        {
            _console.MarkupLine("[red]Error:[/] Not in a git repository.");
            return 1;
        }

        // Check if settings file exists
        if (!_repositoryReader.SettingsFileExists(repoRoot))
        {
            _console.MarkupLine("[red]Error:[/] No .aspire/settings.json file found in repository.");
            return 1;
        }

        // Track imported and skipped resources using a context object
        var context = new ImportContext();
        var visitedRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Load current global config
        var globalConfig = await _globalReader.GetSharedResourcesAsync(cancellationToken);
        var updatedResources = new Dictionary<string, SharedResource>(globalConfig.Resources);

        // Import from current repo and external repos recursively
        var parentDir = _gitService.GetParentDirectory(repoRoot);

        await ImportFromRepositoryAsync(
            repoRoot,
            parentDir,
            visitedRepos,
            updatedResources,
            yes,
            force,
            context,
            cancellationToken);

        // Save updated global config
        var updatedGlobalConfig = new GlobalSharedResources { Resources = updatedResources };
        await _writer.SaveGlobalAsync(updatedGlobalConfig, cancellationToken);

        // Print summary
        _console.WriteLine();
        _console.MarkupLine("[bold]Import Summary:[/]");
        _console.MarkupLine($"  [green]Imported:[/] {context.ImportedCount}");
        _console.MarkupLine($"  [yellow]Skipped (already exists):[/] {context.SkippedCount}");
        _console.MarkupLine($"  [blue]Total:[/] {context.ImportedCount + context.SkippedCount}");

        return 0;
    }

    private async Task ImportFromRepositoryAsync(
        string repoRoot,
        string cloneParentDir,
        HashSet<string> visitedRepos,
        Dictionary<string, SharedResource> resources,
        bool yes,
        bool force,
        ImportContext context,
        CancellationToken cancellationToken)
    {
        // Detect circular reference
        var normalizedRepoPath = Path.GetFullPath(repoRoot);
        if (!visitedRepos.Add(normalizedRepoPath))
        {
            _console.MarkupLine($"[yellow]Skipping circular reference:[/] {normalizedRepoPath}");
            return;
        }

        _console.MarkupLine($"[bold]Importing from {repoRoot}...[/]");

        // Read repository settings
        var repoSettings = await _repositoryReader.ReadAsync(repoRoot, cancellationToken);
        if (repoSettings is null)
        {
            _console.MarkupLine("  [yellow]No settings file found, skipping.[/]");
            return;
        }

        _console.MarkupLine($"  Found {repoSettings.Resources.Count} resources in .aspire/settings.json");

        // Import resources
        foreach (var (id, resource) in repoSettings.Resources)
        {
            if (resources.ContainsKey(id) && !force)
            {
                context.SkippedCount++;
                continue;
            }

            // Convert relative paths to absolute
            var absoluteResource = ConvertToAbsolutePaths(resource, repoRoot);
            resources[id] = absoluteResource;
            context.ImportedCount++;
        }

        // Process external resources
        foreach (var external in repoSettings.ExternalResources)
        {
            await ProcessExternalResourceAsync(
                external,
                cloneParentDir,
                visitedRepos,
                resources,
                yes,
                force,
                context,
                cancellationToken);
        }
    }

    private async Task ProcessExternalResourceAsync(
        ExternalResource external,
        string cloneParentDir,
        HashSet<string> visitedRepos,
        Dictionary<string, SharedResource> resources,
        bool yes,
        bool force,
        ImportContext context,
        CancellationToken cancellationToken)
    {
        _console.WriteLine();
        _console.MarkupLine($"[bold]Cloning external repository:[/] {external.Url} (branch: {external.Branch})...");

        // Determine clone location
        var repoName = GetRepoNameFromUrl(external.Url);

        // Validate repo name to prevent path traversal attacks
        if (string.IsNullOrWhiteSpace(repoName) ||
            repoName is "." or ".." ||
            repoName.Contains('/') ||
            repoName.Contains('\\') ||
            repoName.Contains(Path.DirectorySeparatorChar) ||
            repoName.Contains(Path.AltDirectorySeparatorChar))
        {
            _console.MarkupLine($"  [red]Error: Invalid repository URL - potentially malicious path detected in {external.Url}[/]");
            return;
        }

        var clonePath = Path.Combine(cloneParentDir, repoName);

        // Verify the resolved path stays within the intended directory
        var fullClonePath = Path.GetFullPath(clonePath);
        var fullParentDir = Path.GetFullPath(cloneParentDir);
        if (!fullClonePath.StartsWith(fullParentDir + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            _console.MarkupLine($"  [red]Error: Invalid repository URL - path traversal detected in {external.Url}[/]");
            return;
        }

        // Check if already cloned
        if (_gitService.IsRepositoryCloned(clonePath))
        {
            _console.MarkupLine($"  [yellow]Already cloned at {clonePath}[/]");
        }
        else
        {
            // Confirm clone if not --yes
            if (!yes && !_console.Confirm($"Clone {external.Url} to {clonePath}?", defaultValue: true))
            {
                _console.MarkupLine("  [yellow]Skipped.[/]");
                return;
            }

            _console.MarkupLine($"  Cloning to {clonePath}");
            await _gitService.CloneRepositoryAsync(external.Url, clonePath, external.Branch, cancellationToken);
        }

        // Recursively import from cloned repo
        await ImportFromRepositoryAsync(
            clonePath,
            cloneParentDir,
            visitedRepos,
            resources,
            yes,
            force,
            context,
            cancellationToken);
    }

    private static SharedResource ConvertToAbsolutePaths(SharedResource resource, string repoRoot)
    {
        var containerMode = resource.ContainerMode;
        var projectMode = resource.ProjectMode;

        if (containerMode is not null)
        {
            containerMode = containerMode with
            {
                BuildWorkingDirectory = PathResolver.ToAbsolute(containerMode.BuildWorkingDirectory, repoRoot)
            };
        }

        if (projectMode is not null)
        {
            projectMode = projectMode with
            {
                ProjectPath = PathResolver.ToAbsolute(projectMode.ProjectPath, repoRoot)
            };
        }

        // Add git repository settings if not already present
        var gitRepo = resource.GitRepository;
        if (gitRepo is null)
        {
            // Try to detect git settings from the repo
            var remoteUrl = GetRemoteUrlSync(repoRoot);
            if (remoteUrl is not null && Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri))
            {
                gitRepo = new GitRepositorySettings
                {
                    Url = uri,
                    DefaultBranch = "main"
                };
            }
        }

        return resource with
        {
            ContainerMode = containerMode,
            ProjectMode = projectMode,
            GitRepository = gitRepo
        };
    }

    private static string? GetRemoteUrlSync(string repoRoot)
    {
        try
        {
            var gitConfigPath = Path.Combine(repoRoot, ".git", "config");
            if (!File.Exists(gitConfigPath))
            {
                return null;
            }

            var lines = File.ReadAllLines(gitConfigPath);
            var inRemoteOrigin = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine == "[remote \"origin\"]")
                {
                    inRemoteOrigin = true;
                    continue;
                }

                if (inRemoteOrigin && trimmedLine.StartsWith("url = ", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmedLine["url = ".Length..].Trim();
                }

                if (inRemoteOrigin && trimmedLine.StartsWith('['))
                {
                    break;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetRepoNameFromUrl(string url)
    {
        // Handle various URL formats
        // https://github.com/org/repo.git -> repo
        // git@github.com:org/repo.git -> repo

        var name = url;

        // Remove .git suffix
        if (name.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        // Get last path segment
        var lastSlash = name.LastIndexOf('/');
        if (lastSlash >= 0)
        {
            name = name[(lastSlash + 1)..];
        }

        // Handle SSH format (git@github.com:org/repo)
        var lastColon = name.LastIndexOf(':');
        if (lastColon >= 0)
        {
            name = name[(lastColon + 1)..];
            lastSlash = name.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                name = name[(lastSlash + 1)..];
            }
        }

        return name;
    }

    /// <summary>
    /// Context for tracking import statistics.
    /// </summary>
    private sealed class ImportContext
    {
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
    }
}
