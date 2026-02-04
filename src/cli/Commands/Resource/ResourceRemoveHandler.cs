using Spectre.Console;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli;

/// <summary>
/// Handles the resource remove command execution.
/// </summary>
public sealed class ResourceRemoveHandler
{
    private readonly IAnsiConsole _console;
    private readonly ISharedResourcesWriter _writer;
    private readonly IRepositorySharedResourcesReader _repoReader;
    private readonly IGitService _gitService;
    private readonly IGlobalSharedResourcesReader _globalReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRemoveHandler"/> class.
    /// </summary>
    public ResourceRemoveHandler(
        IAnsiConsole console,
        ISharedResourcesWriter writer,
        IRepositorySharedResourcesReader repoReader,
        IGitService gitService,
        IGlobalSharedResourcesReader globalReader)
    {
        _console = console;
        _writer = writer;
        _repoReader = repoReader;
        _gitService = gitService;
        _globalReader = globalReader;
    }

    /// <summary>
    /// Executes the remove command.
    /// </summary>
    /// <param name="id">The resource identifier to remove.</param>
    /// <param name="yes">Whether to skip the confirmation prompt.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> ExecuteAsync(string id, bool yes, CancellationToken cancellationToken = default)
    {
        var globalResources = await _globalReader.GetSharedResourcesAsync(cancellationToken);

        if (!globalResources.ContainsResource(id))
        {
            _console.MarkupLine($"[red]Error:[/] Resource '[yellow]{id}[/]' not found.");
            return 1;
        }

        // Check if we're in a git repository and if the resource exists there
        string? repoPath = null;
        RepositorySharedResources? repoResources = null;
        bool resourceInRepo = false;

        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            if (_gitService.IsRepositoryCloned(currentDir))
            {
                var gitRepo = await _gitService.GetRepositoryAsync(currentDir, cancellationToken);
                repoPath = gitRepo.RootPath;
                repoResources = await _repoReader.ReadAsync(repoPath, cancellationToken);
                resourceInRepo = repoResources?.ContainsResource(id) == true;
            }
        }
        catch
        {
            // Not in a git repository, or error reading - that's fine
        }

        // Display confirmation prompt if --yes not provided
        if (!yes)
        {
            _console.MarkupLine($"Remove resource '[yellow]{id}[/]'?");
            _console.WriteLine();
            _console.MarkupLine("[grey]This will remove from:[/]");
            _console.MarkupLine($"  [grey]-[/] {GetGlobalConfigPath()}");

            if (resourceInRepo && repoPath is not null)
            {
                _console.MarkupLine($"  [grey]-[/] {_repoReader.GetSettingsFilePath(repoPath)}");
            }

            _console.WriteLine();

            if (!_console.Confirm("Continue?", defaultValue: false))
            {
                return 0;
            }
        }

        // Remove from global config
        var newGlobalResources = globalResources.RemoveResource(id);
        await _writer.SaveGlobalAsync(newGlobalResources, cancellationToken);
        _console.MarkupLine($"[green]Removed '[yellow]{id}[/]' from global config.[/]");

        // Remove from repository settings if present
        if (resourceInRepo && repoResources is not null && repoPath is not null)
        {
            var newRepoResources = repoResources.RemoveResource(id);
            await _writer.SaveRepositoryAsync(newRepoResources, repoPath, cancellationToken);
            _console.MarkupLine($"[green]Removed '[yellow]{id}[/]' from repository settings.[/]");
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
