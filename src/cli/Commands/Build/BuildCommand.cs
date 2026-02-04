using System.CommandLine;

using Spectre.Console;

using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Commands.Build;

/// <summary>
/// Command to build container images for shared resources.
/// </summary>
public sealed class BuildCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "build";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Build container images for shared resources";

    private static readonly Option<string[]?> IdsOption = new("--ids")
    {
        Required = false,
        Arity = ArgumentArity.ZeroOrMore,
        Description = "Resource IDs to build (builds all if omitted)"
    };

    private static readonly Option<bool> GlobalOption = new("--global", "-g")
    {
        Required = false,
        Description = "Build all resources from global config"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildCommand"/> class.
    /// </summary>
    public BuildCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(IdsOption);
        Options.Add(CommonOptions.Force);
        Options.Add(GlobalOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            var ids = parseResult.GetValue(IdsOption);
            var force = parseResult.GetValue(CommonOptions.Force);
            var global = parseResult.GetValue(GlobalOption);

            var console = AnsiConsole.Console;
            var gitCliResolver = new GitCliResolver();
            var gitService = new GitService(gitCliResolver);
            var runtimeResolver = new ContainerRuntimeResolver();
            var commandRunner = new CommandRunner();
            var containerImageService = new ContainerImageService(runtimeResolver, commandRunner, console);
            var tagGenerator = new ImageTagGenerator(new BranchNameSanitizer());
            var repoReader = new RepositorySharedResourcesReader();
            var globalReader = new GlobalSharedResourcesReader(gitService, tagGenerator);

            var handler = new BuildHandler(
                console,
                gitService,
                containerImageService,
                tagGenerator,
                repoReader,
                globalReader);

            return await handler.ExecuteAsync(ids, force, global, cancellationToken);
        });
    }

    /// <summary>
    /// Initializes a new instance with handler dependencies (for testing).
    /// </summary>
    public BuildCommand(
        IAnsiConsole console,
        IGitService gitService,
        IContainerImageService containerImageService,
        IImageTagGenerator tagGenerator,
        IRepositorySharedResourcesReader repoReader,
        IGlobalSharedResourcesReader globalReader) : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(IdsOption);
        Options.Add(CommonOptions.Force);
        Options.Add(GlobalOption);

        var handler = new BuildHandler(
            console,
            gitService,
            containerImageService,
            tagGenerator,
            repoReader,
            globalReader);

        SetAction(async (parseResult, cancellationToken) =>
        {
            var ids = parseResult.GetValue(IdsOption);
            var force = parseResult.GetValue(CommonOptions.Force);
            var global = parseResult.GetValue(GlobalOption);
            return await handler.ExecuteAsync(ids, force, global, cancellationToken);
        });
    }
}
