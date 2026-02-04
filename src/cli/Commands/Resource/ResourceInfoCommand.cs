using System.CommandLine;

using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli;

/// <summary>
/// Command to show detailed information about a shared resource.
/// </summary>
public sealed class ResourceInfoCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "info";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Show detailed information about a shared resource";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceInfoCommand"/> class.
    /// </summary>
    public ResourceInfoCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(ResourceOptions.Id);

        SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(ResourceOptions.Id) ?? string.Empty;

            var gitCliResolver = new GitCliResolver();
            var gitService = new GitService(gitCliResolver);
            var tagGenerator = new ImageTagGenerator(new BranchNameSanitizer());
            var globalReader = new GlobalSharedResourcesReader(gitService, tagGenerator);

            var resources = await globalReader.GetSharedResourcesAsync(cancellationToken);
            var handler = new ResourceInfoHandler();
            return handler.Execute(id, resources, Console.Out, Console.Error);
        });
    }
}