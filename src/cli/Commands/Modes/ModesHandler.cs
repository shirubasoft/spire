using Spectre.Console;

using Spire.Cli.Services.Configuration;

namespace Spire.Cli;

/// <summary>
/// Handles the modes command execution.
/// </summary>
public sealed class ModesHandler
{
    private readonly IAnsiConsole _console;
    private readonly ISharedResourcesWriter _writer;
    private readonly Func<GlobalSharedResources> _resourcesProvider;

    private const string ExitOption = "Exit";

    /// <summary>
    /// Initializes a new instance of the <see cref="ModesHandler"/> class.
    /// </summary>
    /// <param name="console">The console to use for interactive prompts.</param>
    /// <param name="writer">The writer for persisting changes.</param>
    /// <param name="resourcesProvider">A function that provides the current resources.</param>
    public ModesHandler(
        IAnsiConsole console,
        ISharedResourcesWriter writer,
        Func<GlobalSharedResources> resourcesProvider)
    {
        _console = console;
        _writer = writer;
        _resourcesProvider = resourcesProvider;
    }

    /// <summary>
    /// Executes the modes command in interactive mode.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> ExecuteInteractiveAsync(CancellationToken cancellationToken = default)
    {
        var resources = _resourcesProvider();

        if (resources.Resources.Count == 0)
        {
            _console.MarkupLine("[yellow]No resources configured.[/]");
            return 0;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            resources = _resourcesProvider();
            var choices = resources.Resources
                .Select(kvp => FormatChoice(kvp.Key, kvp.Value.Mode))
                .ToList();
            choices.Add(ExitOption);

            var choice = _console.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a resource to toggle mode:")
                    .AddChoices(choices));

            if (choice == ExitOption)
            {
                return 0;
            }

            var resourceId = ParseResourceId(choice);
            if (resourceId is null || !resources.Resources.TryGetValue(resourceId, out var resource))
            {
                _console.MarkupLine($"[red]Resource not found.[/]");
                return 1;
            }

            var oldMode = resource.Mode;
            var newMode = ToggleMode(oldMode);
            var updatedResource = resource.WithMode(newMode);
            var updatedResources = resources.UpdateResource(resourceId, updatedResource);

            await _writer.SaveGlobalAsync(updatedResources, cancellationToken);

            _console.MarkupLine($"[green]Toggled '{resourceId}' from {oldMode} to {newMode}.[/]");
        }

        return 0;
    }

    /// <summary>
    /// Executes the modes command in non-interactive mode.
    /// </summary>
    /// <param name="id">The resource ID.</param>
    /// <param name="mode">The target mode.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> ExecuteNonInteractiveAsync(string id, Mode mode, CancellationToken cancellationToken = default)
    {
        var resources = _resourcesProvider();

        if (!resources.Resources.TryGetValue(id, out var resource))
        {
            _console.MarkupLine($"[red]Resource '{id}' not found.[/]");
            return 1;
        }

        var oldMode = resource.Mode;
        if (oldMode == mode)
        {
            _console.MarkupLine($"[yellow]Resource '{id}' is already in {mode} mode.[/]");
            return 0;
        }

        var updatedResource = resource.WithMode(mode);
        var updatedResources = resources.UpdateResource(id, updatedResource);

        await _writer.SaveGlobalAsync(updatedResources, cancellationToken);

        _console.MarkupLine($"[green]Set '{id}' to {mode} mode.[/]");
        return 0;
    }

    // Using escaped brackets for Spectre.Console markup: [[ and ]]
    private static string FormatChoice(string id, Mode mode) => $"[[{mode}]] {id}";

    private static string? ParseResourceId(string choice)
    {
        // Format is "[[Mode]] id" (with escaped brackets)
        // Spectre.Console will render this as "[Mode] id"
        var closingBracket = choice.IndexOf("]]");

        // Skip "]] " (2 chars for ]] plus 1 for space = 3 chars) to get the resource id
        const int offsetAfterBrackets = 3;
        if (closingBracket < 0 || closingBracket + offsetAfterBrackets >= choice.Length)
        {
            return null;
        }

        return choice[(closingBracket + offsetAfterBrackets)..].Trim();
    }

    /// <summary>
    /// Toggles the mode between Container and Project.
    /// </summary>
    /// <param name="mode">The current mode.</param>
    /// <returns>The toggled mode.</returns>
    public static Mode ToggleMode(Mode mode) => mode switch
    {
        Mode.Container => Mode.Project,
        Mode.Project => Mode.Container,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown mode")
    };
}