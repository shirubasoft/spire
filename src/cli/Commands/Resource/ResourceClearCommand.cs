using System.CommandLine;

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
    public const string CommandDescription = "Clear all shared resources from the current Git repository or globally";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceClearCommand"/> class.
    /// </summary>
    public ResourceClearCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(CommonOptions.Yes);
        Options.Add(CommonOptions.Level);
        Options.Add(ResourceOptions.Ids);
    }
}
