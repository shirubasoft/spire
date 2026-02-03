using Spectre.Console;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli;

/// <summary>
/// Handles the resource clear command execution.
/// </summary>
public sealed class ResourceClearHandler
{
    private readonly IAnsiConsole _console;
    private readonly ISharedResourcesWriter _writer;
    private readonly IRepositorySharedResourcesReader _repoReader;
    private readonly IGitService _gitService;
    private readonly Func<GlobalSharedResources> _getGlobalResources;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceClearHandler"/> class.
    /// </summary>
    public ResourceClearHandler(
        IAnsiConsole console,
        ISharedResourcesWriter writer,
        IRepositorySharedResourcesReader repoReader,
        IGitService gitService,
        Func<GlobalSharedResources> getGlobalResources)
    {
        _console = console;
        _writer = writer;
        _repoReader = repoReader;
        _gitService = gitService;
        _getGlobalResources = getGlobalResources;
    }

    /// <summary>
    /// Executes the clear command.
    /// </summary>
    /// <param name="ids">The resource identifiers to clear, or null to clear all.</param>
    /// <param name="includeRepo">Whether to also clear from repository settings.</param>
    /// <param name="yes">Whether to skip the confirmation prompt.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> ExecuteAsync(string[]? ids, bool includeRepo, bool yes, CancellationToken cancellationToken = default)
    {
        var globalResources = _getGlobalResources();

        // Validate that specified IDs exist
        if (ids is { Length: > 0 })
        {
            foreach (var id in ids)
            {
                if (!globalResources.ContainsResource(id))
                {
                    _console.MarkupLine($"[red]Error:[/] Resource '[yellow]{id}[/]' not found.");
                    return 1;
                }
            }
        }

        // Check if we're in a git repository
        string? repoPath = null;
        RepositorySharedResources? repoResources = null;

        if (includeRepo)
        {
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                if (_gitService.IsRepositoryCloned(currentDir))
                {
                    var gitRepo = await _gitService.GetRepositoryAsync(currentDir, cancellationToken);
                    repoPath = gitRepo.RootPath;
                    repoResources = await _repoReader.ReadAsync(repoPath, cancellationToken);
                }
            }
            catch
            {
                // Not in a git repository, or error reading - that's fine
            }
        }

        var clearingAll = ids is null || ids.Length == 0;
        var globalCount = clearingAll ? globalResources.Count : ids!.Length;
        var repoCount = repoResources?.Count ?? 0;

        // Display confirmation prompt if --yes not provided
        if (!yes)
        {
            if (clearingAll)
            {
                if (includeRepo)
                {
                    _console.MarkupLine($"Clear all resources from global config and repository settings?");
                }
                else
                {
                    _console.MarkupLine($"Clear all [yellow]{globalCount}[/] resources from global config?");
                }
            }
            else
            {
                _console.MarkupLine($"Clear resources: [yellow]{string.Join(", ", ids!)}[/]?");
            }

            _console.WriteLine();
            _console.MarkupLine("[grey]This will remove from:[/]");
            _console.MarkupLine($"  [grey]-[/] {GetGlobalConfigPath()}");

            if (includeRepo && repoPath is not null)
            {
                _console.MarkupLine($"  [grey]-[/] {_repoReader.GetSettingsFilePath(repoPath)}");
            }

            _console.WriteLine();

            if (!_console.Confirm("Continue?", defaultValue: false))
            {
                return 0;
            }
        }

        // Clear from global config
        var clearedGlobalCount = clearingAll ? globalResources.Count : ids!.Length;
        var newGlobalResources = globalResources.ClearResources(ids);
        await _writer.SaveGlobalAsync(newGlobalResources, cancellationToken);
        _console.MarkupLine($"[green]Cleared {clearedGlobalCount} resources from global config.[/]");

        // Clear from repository settings if requested
        if (includeRepo && repoResources is not null && repoPath is not null)
        {
            var clearedRepoCount = clearingAll ? repoResources.Count : ids!.Count(id => repoResources.ContainsResource(id));
            var newRepoResources = repoResources.ClearResources(ids);
            await _writer.SaveRepositoryAsync(newRepoResources, repoPath, cancellationToken);
            _console.MarkupLine($"[green]Cleared {clearedRepoCount} resources from repository settings.[/]");
        }

        return 0;
    }

    private static string GetGlobalConfigPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "spire",
            "aspire-shared-resources.json");
    }
}
