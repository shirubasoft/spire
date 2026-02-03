using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Options specific to resource commands.
/// </summary>
public static class ResourceOptions
{
    /// <summary>
    /// The unique identifier of a single resource.
    /// </summary>
    public static readonly Option<string> Id = new(name: "--id", "-i")
    {
        Required = true,
        Arity = ArgumentArity.ExactlyOne,
        Description = "The unique identifier of the resource"
    };

    /// <summary>
    /// The unique identifiers of one or more resources (required).
    /// </summary>
    public static readonly Option<string[]> Ids = new(name: "--ids")
    {
        Required = true,
        Arity = ArgumentArity.OneOrMore,
        Description = "The unique identifiers of the resources"
    };

    /// <summary>
    /// The unique identifiers of resources to clear (optional, clears all if omitted).
    /// </summary>
    public static readonly Option<string[]?> ClearIds = new(name: "--ids")
    {
        Required = false,
        Arity = ArgumentArity.ZeroOrMore,
        Description = "Resource IDs to clear (clears all if omitted)"
    };

    /// <summary>
    /// Whether to also clear from repository settings.
    /// </summary>
    public static readonly Option<bool> IncludeRepo = new(name: "--include-repo")
    {
        Required = false,
        Description = "Also clear from repository settings (.aspire/settings.json)"
    };
}
