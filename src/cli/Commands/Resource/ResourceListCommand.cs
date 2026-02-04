using System.CommandLine;

using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli;

/// <summary>
/// Command to list all shared resources.
/// </summary>
public sealed class ResourceListCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "list";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "List all shared resources";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceListCommand"/> class.
    /// </summary>
    public ResourceListCommand() : base(name: CommandName, description: CommandDescription)
    {
        SetAction(async (parseResult, cancellationToken) =>
        {
            var gitCliResolver = new GitCliResolver();
            var gitService = new GitService(gitCliResolver);
            var tagGenerator = new ImageTagGenerator(new BranchNameSanitizer());
            var globalReader = new GlobalSharedResourcesReader(gitService, tagGenerator);

            var resources = await globalReader.GetSharedResourcesAsync(cancellationToken);
            var handler = new ResourceListHandler();
            return handler.Execute(resources, Console.Out);
        });
    }
}