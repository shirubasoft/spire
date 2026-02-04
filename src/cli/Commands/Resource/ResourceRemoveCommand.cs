using System.CommandLine;
using Spectre.Console;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli;

/// <summary>
/// Command to remove a shared resource.
/// </summary>
public sealed class ResourceRemoveCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "remove";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Remove a shared resource";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRemoveCommand"/> class.
    /// </summary>
    public ResourceRemoveCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(ResourceOptions.Id);
        Options.Add(CommonOptions.Yes);

        SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(ResourceOptions.Id) ?? string.Empty;
            var yes = parseResult.GetValue(CommonOptions.Yes);

            var console = AnsiConsole.Console;
            var gitCliResolver = new GitCliResolver();
            var gitService = new GitService(gitCliResolver);
            var writer = new SharedResourcesWriter();
            var repoReader = new RepositorySharedResourcesReader();

            var handler = new ResourceRemoveHandler(
                console,
                writer,
                repoReader,
                gitService,
                SharedResourcesConfigurationExtensions.GetSharedResources);

            return await handler.ExecuteAsync(id, yes, cancellationToken);
        });
    }

    /// <summary>
    /// Initializes a new instance with handler dependencies (for testing).
    /// </summary>
    public ResourceRemoveCommand(
        IAnsiConsole console,
        ISharedResourcesWriter writer,
        IRepositorySharedResourcesReader repoReader,
        IGitService gitService,
        Func<GlobalSharedResources>? getGlobalResources = null) : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(ResourceOptions.Id);
        Options.Add(CommonOptions.Yes);

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            getGlobalResources ?? SharedResourcesConfigurationExtensions.GetSharedResources);

        this.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(ResourceOptions.Id) ?? string.Empty;
            var yes = parseResult.GetValue(CommonOptions.Yes);
            return await handler.ExecuteAsync(id, yes, cancellationToken);
        });
    }
}
