using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Command to generate a shared resource from an existing project or container.
/// </summary>
public sealed class ResourceGenerateCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "generate";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Generate a shared resource from an existing project or container";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceGenerateCommand"/> class.
    /// </summary>
    public ResourceGenerateCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(CommonOptions.Yes);
        Arguments.Add(CommonArguments.AppHostDirectoryOrFilePath);
    }
}