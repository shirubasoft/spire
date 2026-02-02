using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Command to share a resource to an apphost or JSON file.
/// </summary>
public sealed class ResourceShareCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "share";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Share a resource to an apphost or JSON file";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceShareCommand"/> class.
    /// </summary>
    public ResourceShareCommand() : base(name: CommandName, description: CommandDescription)
    {
    }
}
