using Spectre.Console;

using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Commands.Build;

/// <summary>
/// Handler for the build command.
/// </summary>
public sealed class BuildHandler
{
    private readonly IAnsiConsole _console;
    private readonly IGitService _gitService;
    private readonly IContainerImageService _containerImageService;
    private readonly IImageTagGenerator _tagGenerator;
    private readonly IRepositorySharedResourcesReader _repoReader;
    private readonly Func<GlobalSharedResources> _getResources;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildHandler"/> class.
    /// </summary>
    /// <param name="console">The console for output.</param>
    /// <param name="gitService">The Git service.</param>
    /// <param name="containerImageService">The container image service.</param>
    /// <param name="tagGenerator">The image tag generator.</param>
    /// <param name="repoReader">The repository shared resources reader.</param>
    /// <param name="getResources">Function to get shared resources.</param>
    public BuildHandler(
        IAnsiConsole console,
        IGitService gitService,
        IContainerImageService containerImageService,
        IImageTagGenerator tagGenerator,
        IRepositorySharedResourcesReader repoReader,
        Func<GlobalSharedResources> getResources)
    {
        _console = console;
        _gitService = gitService;
        _containerImageService = containerImageService;
        _tagGenerator = tagGenerator;
        _repoReader = repoReader;
        _getResources = getResources;
    }

    /// <summary>
    /// Executes the build command.
    /// </summary>
    /// <param name="ids">The resource IDs to build, or null/empty to auto-resolve.</param>
    /// <param name="force">Whether to force rebuild even if image exists.</param>
    /// <param name="global">Whether to build all resources from global config.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> ExecuteAsync(string[]? ids, bool force, bool global, CancellationToken cancellationToken)
    {
        var resources = _getResources();

        var resolvedIds = await ResolveIdsAsync(ids, global, resources, cancellationToken);
        if (resolvedIds is null)
        {
            return 0;
        }

        if (resolvedIds.Length == 0)
        {
            _console.MarkupLine("[yellow]No buildable resources found.[/]");
            return 0;
        }

        var builtCount = 0;
        var skippedCount = 0;
        var errors = new List<string>();

        _console.MarkupLine($"[bold]Building images for:[/] {string.Join(", ", resolvedIds)}");
        _console.WriteLine();

        foreach (var id in resolvedIds)
        {
            var result = await BuildResourceAsync(id, resources, force, cancellationToken);

            switch (result)
            {
                case BuildResult.BuiltResult:
                    builtCount++;
                    break;
                case BuildResult.SkippedResult:
                    skippedCount++;
                    break;
                case BuildResult.ErrorResult error:
                    errors.Add($"{id}: {error.Message}");
                    break;
            }

            _console.WriteLine();
        }

        // Print summary
        _console.MarkupLine("[bold]Summary:[/]");
        _console.MarkupLine($"  Built: {builtCount}");
        _console.MarkupLine($"  Skipped: {skippedCount}");

        if (errors.Count > 0)
        {
            _console.MarkupLine($"  [red]Errors: {errors.Count}[/]");
            foreach (var error in errors)
            {
                _console.MarkupLine($"    [red]{error.EscapeMarkup()}[/]");
            }
            return 1;
        }

        return 0;
    }

    private async Task<string[]?> ResolveIdsAsync(
        string[]? ids,
        bool global,
        GlobalSharedResources resources,
        CancellationToken cancellationToken)
    {
        // If explicit IDs were provided, use them directly
        if (ids is { Length: > 0 })
        {
            return ids;
        }

        // Auto-resolve: --global flag means use all global resources with ContainerMode
        if (global)
        {
            _console.MarkupLine("[bold]Building from global config[/]");
            return resources.Resources
                .Where(kvp => kvp.Value.ContainerMode is not null)
                .Select(kvp => kvp.Key)
                .ToArray();
        }

        // Auto-resolve: check if we're in a repository
        var currentDir = Directory.GetCurrentDirectory();
        try
        {
            var repoRoot = await _gitService.GetRepositoryRootAsync(currentDir, cancellationToken);
            if (repoRoot is not null)
            {
                var repoResources = await _repoReader.ReadAsync(repoRoot, cancellationToken);

                if (repoResources is not null && repoResources.Resources.Count > 0)
                {
                    _console.MarkupLine("[bold]Building from repository settings[/]");
                    // Use repo resource keys, but filter to only those with ContainerMode in global config
                    return repoResources.Resources.Keys
                        .Where(key => resources.Resources.TryGetValue(key, out var r) && r.ContainerMode is not null)
                        .ToArray();
                }

                // In a repo but no resources configured in .aspire/settings.json
                _console.MarkupLine("No shared resources found in repository settings. Use [yellow]--global[/] to build from global config, or [yellow]--ids[/] to specify resources.");
                return null;
            }
        }
        catch
        {
            // Error reading git or repo settings - fall through
        }

        // Not in a repo and no --global flag
        _console.MarkupLine("Not in a repository. Use [yellow]--global[/] to build all resources from global config.");
        return null;
    }

    private async Task<BuildResult> BuildResourceAsync(
        string id,
        GlobalSharedResources resources,
        bool force,
        CancellationToken cancellationToken)
    {
        _console.MarkupLine($"[bold blue][[{id.EscapeMarkup()}]][/]");

        // Find the resource
        if (!resources.Resources.TryGetValue(id, out var resource))
        {
            _console.MarkupLine($"  [red]Error: Resource not found[/]");
            return new BuildResult.ErrorResult("Resource not found");
        }

        // Check for container mode settings
        if (resource.ContainerMode is null)
        {
            _console.MarkupLine($"  [red]Error: Resource does not have container mode settings[/]");
            return new BuildResult.ErrorResult("Resource does not have container mode settings");
        }

        var containerSettings = resource.ContainerMode;

        // Check for build command
        if (string.IsNullOrWhiteSpace(containerSettings.BuildCommand))
        {
            _console.MarkupLine($"  [red]Error: Resource does not have a build command[/]");
            return new BuildResult.ErrorResult("Resource does not have a build command");
        }

        // Get git repository info
        var repoPath = containerSettings.BuildWorkingDirectory;
        GitRepository repository;
        try
        {
            repository = await _gitService.GetRepositoryAsync(repoPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"  [red]Error getting git info: {ex.Message.EscapeMarkup()}[/]");
            return new BuildResult.ErrorResult($"Error getting git info: {ex.Message}");
        }

        var tags = _tagGenerator.Generate(repository);

        _console.MarkupLine($"  Branch: {repository.CurrentBranch.EscapeMarkup()}");
        _console.MarkupLine($"  Commit: {tags.CommitTag.EscapeMarkup()}");
        _console.MarkupLine($"  ContainerMode.ImageRegistry: {containerSettings.ImageRegistry.EscapeMarkup()}");
        _console.MarkupLine($"  ContainerMode.ImageName: {containerSettings.ImageName.EscapeMarkup()}");

        // Check if commit tag already exists (only commit tag, not branch or latest)
        _console.MarkupLine("  Checking existing tags...");

        var tagExists = await _containerImageService.TagExistsAsync(
            containerSettings.ImageRegistry,
            containerSettings.ImageName,
            tags.CommitTag,
            cancellationToken);

        if (tagExists && !force)
        {
            _console.MarkupLine($"  [yellow]Tag {tags.CommitTag.EscapeMarkup()} exists, skipping (use --force to rebuild)[/]");
            return new BuildResult.SkippedResult();
        }

        if (tagExists && force)
        {
            _console.MarkupLine($"  [yellow]Tag {tags.CommitTag.EscapeMarkup()} exists, rebuilding (--force)[/]");
        }
        else
        {
            _console.MarkupLine($"  Tag {tags.CommitTag.EscapeMarkup()} not found, building...");
        }

        // Build the image
        _console.MarkupLine($"  [grey]Running: {containerSettings.BuildCommand.EscapeMarkup()}[/]");

        try
        {
            var buildRequest = new ContainerImageBuildRequest
            {
                Image = new ContainerImage
                {
                    ImageName = containerSettings.ImageName,
                    ImageRegistry = containerSettings.ImageRegistry,
                    ImageTag = tags.CommitTag
                },
                Command = containerSettings.BuildCommand,
                WorkingDirectory = containerSettings.BuildWorkingDirectory,
                AdditionalTags = [tags.BranchTag, tags.LatestTag]
            };

            await _containerImageService.BuildImageAsync(buildRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"  [red]Build failed: {ex.Message.EscapeMarkup()}[/]");
            return new BuildResult.ErrorResult($"Build failed: {ex.Message}");
        }

        _console.MarkupLine("  [green]Build completed.[/]");

        // Apply all tags to the image (dotnet publish creates the image without registry prefix)
        var sourceImage = $"{containerSettings.ImageName}:{tags.LatestTag}";

        try
        {
            await _containerImageService.TagImageAsync(sourceImage, [tags.CommitTag, tags.BranchTag], cancellationToken);
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"  [red]Tagging failed: {ex.Message.EscapeMarkup()}[/]");
            return new BuildResult.ErrorResult($"Tagging failed: {ex.Message}");
        }

        _console.MarkupLine($"  [green]Tags applied: {string.Join(", ", tags.All)}[/]");
        _console.MarkupLine($"  [green]Updated ContainerMode.ImageTag: {tags.BranchTag.EscapeMarkup()}[/]");

        return new BuildResult.BuiltResult();
    }

    private abstract record BuildResult
    {
        private BuildResult() { }

        public sealed record BuiltResult : BuildResult;
        public sealed record SkippedResult : BuildResult;
        public sealed record ErrorResult(string Message) : BuildResult;
    }
}
