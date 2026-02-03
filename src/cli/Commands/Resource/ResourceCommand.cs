using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Command to manage shared resources.
/// </summary>
public sealed class ResourceCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "resource";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Manage shared resources";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceCommand"/> class.
    /// </summary>
    public ResourceCommand() : base(name: CommandName, description: CommandDescription)
    {
        Subcommands.Add(new ResourceListCommand());
        Subcommands.Add(new ResourceInfoCommand());
        Subcommands.Add(new ResourceGenerateCommand());
        Subcommands.Add(new ResourceImportCommand());
        Subcommands.Add(new ResourceRemoveCommand());
        Subcommands.Add(new ResourceClearCommand());
    }
}