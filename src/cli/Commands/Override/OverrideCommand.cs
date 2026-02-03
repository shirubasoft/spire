using System;
using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Command to override configurations for shared resources.
/// </summary>
public sealed class OverrideCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "override";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Override configurations for shared resources for the current Git repository or globally";

    /// <summary>
    /// Initializes a new instance of the <see cref="OverrideCommand"/> class.
    /// </summary>
    public OverrideCommand() : base(name: CommandName, description: CommandDescription)
    {

    }
}