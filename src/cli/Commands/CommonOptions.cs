using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Common options shared across multiple commands.
/// </summary>
public static class CommonOptions
{
    /// <summary>
    /// Enables interactive mode.
    /// </summary>
    public static readonly Option<bool> Interactive = new(name: "--interactive", "-i")
    {
        Description = "Enable interactive mode",
        Required = false
    };

    /// <summary>
    /// The level at which to apply the command (Repo or Global).
    /// </summary>
    public static readonly Option<Level> Level = new(name: "--level", "-l")
    {
        Required = false,
        Arity = ArgumentArity.ExactlyOne,
        DefaultValueFactory = _ => Cli.Level.Repo,
        Description = "The level to apply the command at (Repo, Global)"
    };

    /// <summary>
    /// Automatic yes to prompts; runs non-interactively.
    /// </summary>
    public static readonly Option<bool> Yes = new(name: "--yes", "-y")
    {
        Description = "Automatic yes to prompts; assume 'yes' as answer to all prompts and run non-interactively",
        Required = false
    };
}
