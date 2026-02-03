using System.CommandLine;

using Spectre.Console;

using Spire.Cli.Commands.Resource.Handlers;
using Spire.Cli.Services.Analysis;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli;

/// <summary>
/// Command to generate a shared resource from an existing project or container.
/// </summary>
public sealed class ResourceGenerateCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "generate";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Generate a shared resource from an existing project or container";

    private static readonly Option<string?> IdOption = new("--id")
    {
        Description = "Resource identifier (prompted if not provided)"
    };

    private static readonly Option<string?> ImageNameOption = new("--image-name")
    {
        Description = "Override container image name (default: project name)"
    };

    private static readonly Option<string?> ImageRegistryOption = new("--image-registry")
    {
        Description = "Override registry (default: docker.io)"
    };

    private static readonly Argument<string> PathArgument = new("path")
    {
        Description = "Path to the .csproj file, Dockerfile, or directory"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceGenerateCommand"/> class.
    /// </summary>
    public ResourceGenerateCommand() : base(name: CommandName, description: CommandDescription)
    {
        Arguments.Add(PathArgument);
        Options.Add(IdOption);
        Options.Add(ImageNameOption);
        Options.Add(ImageRegistryOption);
        Options.Add(CommonOptions.Yes);

        SetAction(async (parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(PathArgument)!;
            var id = parseResult.GetValue(IdOption);
            var imageName = parseResult.GetValue(ImageNameOption);
            var imageRegistry = parseResult.GetValue(ImageRegistryOption);
            var yes = parseResult.GetValue(CommonOptions.Yes);

            var console = AnsiConsole.Console;
            var gitCliResolver = new GitCliResolver();
            var gitService = new GitService(gitCliResolver);
            var projectAnalyzer = new ProjectAnalyzer();
            var dockerfileAnalyzer = new DockerfileAnalyzer();
            var gitSettingsDetector = new GitSettingsDetector(gitService);
            var writer = new SharedResourcesWriter();
            var repositoryReader = new RepositorySharedResourcesReader();

            var handler = new ResourceGenerateHandler(
                console,
                gitService,
                projectAnalyzer,
                dockerfileAnalyzer,
                gitSettingsDetector,
                writer,
                repositoryReader);

            return await handler.ExecuteAsync(path, id, imageName, imageRegistry, yes, cancellationToken);
        });
    }
}