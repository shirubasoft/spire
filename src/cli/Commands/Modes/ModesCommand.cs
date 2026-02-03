using System.CommandLine;

using Spectre.Console;

using Spire.Cli.Services.Configuration;

namespace Spire.Cli;

/// <summary>
/// Command to manage modes for shared resources.
/// </summary>
public sealed class ModesCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "modes";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Manage modes for shared resources";

    private readonly Option<string?> _idOption = new("--id", "-i")
    {
        Description = "Resource ID (for non-interactive use)"
    };

    private readonly Option<Mode?> _modeOption = new("--mode", "-m")
    {
        Description = "Target mode: Container or Project (for non-interactive use)"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ModesCommand"/> class.
    /// </summary>
    public ModesCommand() : this(AnsiConsole.Console, new SharedResourcesWriter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModesCommand"/> class with dependency injection.
    /// </summary>
    /// <param name="console">The console to use for interactive prompts.</param>
    /// <param name="writer">The writer for persisting changes.</param>
    public ModesCommand(IAnsiConsole console, ISharedResourcesWriter writer)
        : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(_idOption);
        Options.Add(_modeOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(_idOption);
            var mode = parseResult.GetValue(_modeOption);

            // Validation: --id and --mode must be provided together
            if (id is not null && mode is null)
            {
                console.MarkupLine("[red]Error: When --id is provided, --mode must also be specified.[/]");
                return 1;
            }

            if (mode is not null && id is null)
            {
                console.MarkupLine("[red]Error: When --mode is provided, --id must also be specified.[/]");
                return 1;
            }

            var handler = new ModesHandler(console, writer, SharedResourcesConfigurationExtensions.GetSharedResources);

            if (id is not null && mode is not null)
            {
                return await handler.ExecuteNonInteractiveAsync(id, mode.Value, cancellationToken);
            }

            return await handler.ExecuteInteractiveAsync(cancellationToken);
        });
    }
}