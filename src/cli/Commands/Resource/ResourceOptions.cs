using System;
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
    /// The unique identifiers of one or more resources.
    /// </summary>
    public static readonly Option<string[]> Ids = new(name: "--ids")
    {
        Required = true,
        Arity = ArgumentArity.OneOrMore,
        Description = "The unique identifiers of the resources"
    };
}
