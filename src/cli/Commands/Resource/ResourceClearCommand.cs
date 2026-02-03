using System.CommandLine;
using Spectre.Console;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli;

/// <summary>
/// Command to clear all shared resources.
/// </summary>
public sealed class ResourceClearCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "clear";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Clear all shared resources";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceClearCommand"/> class.
    /// </summary>
    public ResourceClearCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(ResourceOptions.ClearIds);
        Options.Add(ResourceOptions.IncludeRepo);
        Options.Add(CommonOptions.Yes);
    }

    /// <summary>
    /// Initializes a new instance with handler dependencies.
    /// </summary>
    public ResourceClearCommand(
        IAnsiConsole console,
        ISharedResourcesWriter writer,
        IRepositorySharedResourcesReader repoReader,
        IGitService gitService,
        Func<GlobalSharedResources>? getGlobalResources = null) : this()
    {
        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            getGlobalResources ?? SharedResourcesConfigurationExtensions.GetSharedResources);

        this.SetAction(async (parseResult, cancellationToken) =>
        {
            var ids = parseResult.GetValue(ResourceOptions.ClearIds);
            var includeRepo = parseResult.GetValue(ResourceOptions.IncludeRepo);
            var yes = parseResult.GetValue(CommonOptions.Yes);
            return await handler.ExecuteAsync(ids, includeRepo, yes, cancellationToken);
        });
    }
}