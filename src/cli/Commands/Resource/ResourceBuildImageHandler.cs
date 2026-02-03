using Spectre.Console;

using Spire.Cli.Services;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Commands.Resource;

/// <summary>
/// Handler for the resource build-image command.
/// </summary>
public sealed class ResourceBuildImageHandler
{
    private readonly IAnsiConsole _console;
    private readonly IGitService _gitService;
    private readonly IContainerImageService _containerImageService;
    private readonly IImageTagGenerator _tagGenerator;
    private readonly Func<GlobalSharedResources> _getResources;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceBuildImageHandler"/> class.
    /// </summary>
    /// <param name="console">The console for output.</param>
    /// <param name="gitService">The Git service.</param>
    /// <param name="containerImageService">The container image service.</param>
    /// <param name="tagGenerator">The image tag generator.</param>
    /// <param name="getResources">Function to get shared resources.</param>
    public ResourceBuildImageHandler(
        IAnsiConsole console,
        IGitService gitService,
        IContainerImageService containerImageService,
        IImageTagGenerator tagGenerator,
        Func<GlobalSharedResources> getResources)
    {
        _console = console;
        _gitService = gitService;
        _containerImageService = containerImageService;
        _tagGenerator = tagGenerator;
        _getResources = getResources;
    }

    /// <summary>
    /// Executes the build-image command.
    /// </summary>
    /// <param name="ids">The resource IDs to build.</param>
    /// <param name="force">Whether to force rebuild even if image exists.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> ExecuteAsync(string[] ids, bool force, CancellationToken cancellationToken)
    {
        var resources = _getResources();
        var builtCount = 0;
        var skippedCount = 0;
        var errors = new List<string>();

        _console.MarkupLine($"[bold]Building images for:[/] {string.Join(", ", ids)}");
        _console.WriteLine();

        foreach (var id in ids)
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

        // Apply all tags to the image
        var sourceImage = string.IsNullOrEmpty(containerSettings.ImageRegistry)
            ? $"{containerSettings.ImageName}:{tags.CommitTag}"
            : $"{containerSettings.ImageRegistry}/{containerSettings.ImageName}:{tags.CommitTag}";

        try
        {
            await _containerImageService.TagImageAsync(sourceImage, [tags.BranchTag, tags.LatestTag], cancellationToken);
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