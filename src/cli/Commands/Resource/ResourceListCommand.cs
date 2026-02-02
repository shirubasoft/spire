using System.CommandLine;

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
        Options.Add(CommonOptions.Level);
    }
}
