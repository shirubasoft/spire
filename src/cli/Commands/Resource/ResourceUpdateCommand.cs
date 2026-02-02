using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Command to update an existing shared resource.
/// </summary>
public sealed class ResourceUpdateCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "update";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Update an existing shared resource";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUpdateCommand"/> class.
    /// </summary>
    public ResourceUpdateCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(CommonOptions.Yes);
    }
}
