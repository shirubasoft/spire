using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Command to manage modes for shared resources.
/// </summary>
public sealed class ModesCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "modes";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Manage modes for shared resources";

    /// <summary>
    /// Initializes a new instance of the <see cref="ModesCommand"/> class.
    /// </summary>
    public ModesCommand() : base(name: CommandName, description: CommandDescription)
    {
    }
}
