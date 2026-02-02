using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Common arguments shared across multiple commands.
/// </summary>
public static class CommonArguments
{
    /// <summary>
    /// The path to the apphost directory or file.
    /// </summary>
    public static Argument<string> AppHostDirectoryOrFilePath = new Argument<string>(name: "apphost-directory-or-file-path")
    {
        Arity = ArgumentArity.ExactlyOne,
        Description = "The path to the apphost directory or file"
    };
}
