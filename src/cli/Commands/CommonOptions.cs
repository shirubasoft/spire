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
    /// Output as JSON.
    /// </summary>
    public static readonly Option<bool> Json = new(name: "--json")
    {
        Description = "Output as JSON",
        Required = false
    };

    /// <summary>
    /// Automatic yes to prompts; runs non-interactively.
    /// </summary>
    public static readonly Option<bool> Yes = new(name: "--yes", "-y")
    {
        Description = "Automatic yes to prompts; assume 'yes' as answer to all prompts and run non-interactively",
        Required = false
    };

    /// <summary>
    /// Force the operation to run.
    /// </summary>
    public static readonly Option<bool> Force = new(name: "--force", "-f")
    {
        Description = "Force the operation to run",
        Required = false
    };
}