using System;
using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Provides the top-level commands for the CLI application.
/// </summary>
public static class SpireCli
{
    /// <summary>
    /// Parses the command-line arguments and invokes the matching command.
    /// </summary>
    public static Task<int> RunAsync(string[] args)
    {
        RootCommand rootCommand = new("Spire CLI");

        rootCommand.Subcommands.Add(new ModesCommand());
        rootCommand.Subcommands.Add(new ResourceCommand());
        rootCommand.Subcommands.Add(new OverrideCommand());

        return rootCommand.Parse(args).InvokeAsync();
    }
}