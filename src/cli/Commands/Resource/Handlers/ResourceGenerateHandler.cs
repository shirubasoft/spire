using Spectre.Console;

using Spire.Cli.Services.Analysis;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Commands.Resource.Handlers;

/// <summary>
/// Handles the resource generate command.
/// </summary>
public sealed class ResourceGenerateHandler
{
    private readonly IAnsiConsole _console;
    private readonly IGitService _gitService;
    private readonly IProjectAnalyzer _projectAnalyzer;
    private readonly IDockerfileAnalyzer _dockerfileAnalyzer;
    private readonly IGitSettingsDetector _gitSettingsDetector;
    private readonly ISharedResourcesWriter _writer;
    private readonly IRepositorySharedResourcesReader _repositoryReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceGenerateHandler"/> class.
    /// </summary>
    public ResourceGenerateHandler(
        IAnsiConsole console,
        IGitService gitService,
        IProjectAnalyzer projectAnalyzer,
        IDockerfileAnalyzer dockerfileAnalyzer,
        IGitSettingsDetector gitSettingsDetector,
        ISharedResourcesWriter writer,
        IRepositorySharedResourcesReader repositoryReader)
    {
        _console = console;
        _gitService = gitService;
        _projectAnalyzer = projectAnalyzer;
        _dockerfileAnalyzer = dockerfileAnalyzer;
        _gitSettingsDetector = gitSettingsDetector;
        _writer = writer;
        _repositoryReader = repositoryReader;
    }

    /// <summary>
    /// Executes the generate command.
    /// </summary>
    /// <param name="path">The path to the project or Dockerfile.</param>
    /// <param name="id">The resource ID (optional, will prompt if not provided).</param>
    /// <param name="imageName">Override for the container image name.</param>
    /// <param name="imageRegistry">Override for the container image registry.</param>
    /// <param name="yes">Skip confirmation prompts.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> ExecuteAsync(
        string path,
        string? id,
        string? imageName,
        string? imageRegistry,
        bool yes,
        CancellationToken cancellationToken = default)
    {
        // Validate path exists
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            _console.MarkupLine($"[red]Error:[/] Path does not exist: {fullPath}");
            return 1;
        }

        _console.MarkupLine($"[bold]Analyzing {fullPath}...[/]");
        _console.WriteLine();

        // Try to analyze as project first, then as Dockerfile
        var projectResult = await _projectAnalyzer.AnalyzeAsync(fullPath, cancellationToken);
        var dockerfileResult = await _dockerfileAnalyzer.AnalyzeAsync(fullPath, cancellationToken);

        if (projectResult is null && dockerfileResult is null)
        {
            _console.MarkupLine("[red]Error:[/] No .csproj file or Dockerfile found at the specified path.");
            return 1;
        }

        // Detect git settings
        var gitSettings = await _gitSettingsDetector.DetectAsync(fullPath, cancellationToken);

        // Prompt for ID if not provided
        var resourceId = id ?? PromptForId(projectResult, dockerfileResult);
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            _console.MarkupLine("[red]Error:[/] Resource ID is required.");
            return 1;
        }

        // Build the resource configurations
        var (globalResource, repoResource) = BuildResources(
            projectResult,
            dockerfileResult,
            gitSettings,
            resourceId,
            imageName,
            imageRegistry);

        // Display detected settings
        DisplayDetectedSettings(resourceId, projectResult, dockerfileResult, globalResource);

        // Confirm save
        if (!yes && !_console.Confirm("Save to repository settings and global config?", defaultValue: true))
        {
            _console.MarkupLine("[yellow]Cancelled.[/]");
            return 0;
        }

        // Save to both locations
        await SaveResourcesAsync(
            resourceId,
            globalResource,
            repoResource,
            gitSettings?.RepositoryRoot ?? Directory.GetCurrentDirectory(),
            cancellationToken);

        return 0;
    }

    private string PromptForId(ProjectAnalysisResult? project, DockerfileAnalysisResult? dockerfile)
    {
        var defaultId = project?.ProjectName.ToLowerInvariant()
            ?? dockerfile?.SuggestedImageName
            ?? "my-resource";

        return _console.Prompt(
            new TextPrompt<string>("Enter resource ID:")
                .DefaultValue(defaultId));
    }

    private static (SharedResource global, SharedResource repo) BuildResources(
        ProjectAnalysisResult? project,
        DockerfileAnalysisResult? dockerfile,
        GitSettingsResult? gitSettings,
        string resourceId,
        string? imageNameOverride,
        string? imageRegistryOverride)
    {
        var mode = project is not null ? Mode.Project : Mode.Container;
        var imageName = imageNameOverride ?? project?.ProjectName.ToLowerInvariant() ?? dockerfile?.SuggestedImageName ?? resourceId;
        var imageRegistry = imageRegistryOverride ?? "docker.io";
        var imageTag = "latest";

        // Determine paths
        string absoluteWorkingDirectory;
        string relativeWorkingDirectory;
        string buildCommand;

        if (project is not null)
        {
            absoluteWorkingDirectory = project.ProjectDirectory;
            buildCommand = project.BuildCommand;
            relativeWorkingDirectory = gitSettings is not null
                ? PathResolver.ToRelative(project.ProjectDirectory, gitSettings.RepositoryRoot)
                : project.ProjectDirectory;
        }
        else
        {
            absoluteWorkingDirectory = dockerfile!.BuildContext;
            buildCommand = dockerfile.BuildCommand;
            relativeWorkingDirectory = gitSettings is not null
                ? PathResolver.ToRelative(dockerfile.BuildContext, gitSettings.RepositoryRoot)
                : dockerfile.BuildContext;
        }

        // Global resource with absolute paths
        var globalResource = new SharedResource
        {
            Mode = mode,
            ContainerMode = new ContainerModeSettings
            {
                ImageName = imageName,
                ImageRegistry = imageRegistry,
                ImageTag = imageTag,
                BuildCommand = buildCommand,
                BuildWorkingDirectory = absoluteWorkingDirectory
            },
            ProjectMode = project is not null
                ? new ProjectModeSettings { ProjectDirectory = absoluteWorkingDirectory }
                : null,
            GitRepository = gitSettings is not null
                ? new GitRepositorySettings { Url = gitSettings.RemoteUrl, DefaultBranch = gitSettings.DefaultBranch }
                : null
        };

        // Repository resource with relative paths (no gitRepository section)
        var repoResource = new SharedResource
        {
            Mode = mode,
            ContainerMode = new ContainerModeSettings
            {
                ImageName = imageName,
                ImageRegistry = imageRegistry,
                ImageTag = imageTag,
                BuildCommand = buildCommand,
                BuildWorkingDirectory = relativeWorkingDirectory
            },
            ProjectMode = project is not null
                ? new ProjectModeSettings { ProjectDirectory = relativeWorkingDirectory }
                : null,
            GitRepository = null
        };

        return (globalResource, repoResource);
    }

    private void DisplayDetectedSettings(
        string resourceId,
        ProjectAnalysisResult? project,
        DockerfileAnalysisResult? dockerfile,
        SharedResource resource)
    {
        _console.MarkupLine("[bold]Detected settings:[/]");
        _console.MarkupLine($"  [blue]ID:[/] {resourceId}");
        _console.MarkupLine($"  [blue]Mode:[/] {(project is not null ? "Project + Container" : "Container")}");

        if (project is not null)
        {
            _console.MarkupLine($"  [blue]ProjectMode.ProjectDirectory:[/] {resource.ProjectMode?.ProjectDirectory}");
        }

        if (resource.ContainerMode is not null)
        {
            _console.MarkupLine($"  [blue]ContainerMode.ImageName:[/] {resource.ContainerMode.ImageName}");
            _console.MarkupLine($"  [blue]ContainerMode.ImageRegistry:[/] {resource.ContainerMode.ImageRegistry}");
            _console.MarkupLine($"  [blue]ContainerMode.ImageTag:[/] {resource.ContainerMode.ImageTag}");
            _console.MarkupLine($"  [blue]ContainerMode.BuildCommand:[/] {resource.ContainerMode.BuildCommand}");
            _console.MarkupLine($"  [blue]ContainerMode.BuildWorkingDirectory:[/] {resource.ContainerMode.BuildWorkingDirectory}");
        }

        if (resource.GitRepository is not null)
        {
            _console.MarkupLine($"  [blue]GitRepository.Url:[/] {resource.GitRepository.Url}");
            _console.MarkupLine($"  [blue]GitRepository.DefaultBranch:[/] {resource.GitRepository.DefaultBranch}");
        }

        _console.WriteLine();
    }

    private async Task SaveResourcesAsync(
        string resourceId,
        SharedResource globalResource,
        SharedResource repoResource,
        string repoRoot,
        CancellationToken cancellationToken)
    {
        // Load and update global config
        var globalConfig = SharedResourcesConfigurationExtensions.GetSharedResources();
        var updatedGlobalResources = new Dictionary<string, SharedResource>(globalConfig.Resources)
        {
            [resourceId] = globalResource
        };
        var updatedGlobalConfig = new GlobalSharedResources { Resources = updatedGlobalResources };
        await _writer.SaveGlobalAsync(updatedGlobalConfig, cancellationToken);

        // Load and update repository settings
        var repoSettings = await _repositoryReader.ReadAsync(repoRoot, cancellationToken)
            ?? new RepositorySharedResources { Resources = [] };

        var updatedRepoResources = new Dictionary<string, SharedResource>(repoSettings.Resources)
        {
            [resourceId] = repoResource
        };
        var updatedRepoSettings = new RepositorySharedResources
        {
            Resources = updatedRepoResources,
            ExternalResources = repoSettings.ExternalResources
        };
        await _writer.SaveRepositoryAsync(updatedRepoSettings, repoRoot, cancellationToken);

        _console.WriteLine();
        _console.MarkupLine("[green]Saved to:[/]");
        _console.MarkupLine($"  - {SharedResourcesWriter.GetRepositorySettingsPath(repoRoot)}");
        _console.MarkupLine($"  - {SharedResourcesWriter.GetGlobalConfigPath()}");
    }
}
