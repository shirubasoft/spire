using System.CommandLine;

namespace Spire.Cli.Commands.Resource;

/// <summary>
/// Command to build a container image for a shared resource.
/// </summary>
public sealed class ResourceBuildImageCommand : Command
{
    /// <summary>
    /// The name of the command.
    /// </summary>
    public const string CommandName = "build-image";

    /// <summary>
    /// The description of the command.
    /// </summary>
    public const string CommandDescription = "Builds a container image for a shared resource.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceBuildImageCommand"/> class.
    /// </summary>
    /// <param name="handler">The handler for the command.</param>
    public ResourceBuildImageCommand(ResourceBuildImageHandler handler)
        : base(CommandName, CommandDescription)
    {
        Options.Add(ResourceOptions.Ids);
        Options.Add(CommonOptions.Force);

        this.SetAction(async (parseResult, cancellationToken) =>
        {
            var ids = parseResult.GetValue(ResourceOptions.Ids) ?? [];
            var force = parseResult.GetValue(CommonOptions.Force);
            return await handler.ExecuteAsync(ids, force, cancellationToken);
        });
    }
}