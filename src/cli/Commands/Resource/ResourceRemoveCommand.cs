using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Command to remove a shared resource.
/// </summary>
public sealed class ResourceRemoveCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "remove";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Remove a shared resource";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRemoveCommand"/> class.
    /// </summary>
    public ResourceRemoveCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(CommonOptions.Yes);
    }
}