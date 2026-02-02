using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Command to add a new shared resource.
/// </summary>
public sealed class ResourceAddCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "add";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Add a new shared resource";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAddCommand"/> class.
    /// </summary>
    public ResourceAddCommand() : base(name: CommandName, description: CommandDescription)
    {
    }
}
