using System.CommandLine;

using Spectre.Console;

using Spire.Cli.Commands.Resource.Handlers;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli;

/// <summary>
/// Command to import shared resources from .aspire-shared-resources.json files in the current git repository.
/// </summary>
public sealed class ResourceImportCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "import";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Import shared resources from .aspire/settings.json files in the current git repository";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceImportCommand"/> class.
    /// </summary>
    public ResourceImportCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(CommonOptions.Yes);
        Options.Add(CommonOptions.Force);

        SetAction(async (parseResult, cancellationToken) =>
        {
            var yes = parseResult.GetValue(CommonOptions.Yes);
            var force = parseResult.GetValue(CommonOptions.Force);

            var console = AnsiConsole.Console;
            var gitCliResolver = new GitCliResolver();
            var gitService = new GitService(gitCliResolver);
            var repositoryReader = new RepositorySharedResourcesReader();
            var writer = new SharedResourcesWriter();
            var tagGenerator = new ImageTagGenerator(new BranchNameSanitizer());
            var globalReader = new GlobalSharedResourcesReader(gitService, tagGenerator);

            var handler = new ResourceImportHandler(console, gitService, repositoryReader, writer, globalReader);
            return await handler.ExecuteAsync(yes, force, cancellationToken);
        });
    }
}