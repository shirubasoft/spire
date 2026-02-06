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
    private readonly IGlobalSharedResourcesReader _globalReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModesHandler"/> class.
    /// </summary>
    /// <param name="console">The console to use for interactive prompts.</param>
    /// <param name="writer">The writer for persisting changes.</param>
    /// <param name="globalReader">The global shared resources reader.</param>
    public ModesHandler(
        IAnsiConsole console,
        ISharedResourcesWriter writer,
        IGlobalSharedResourcesReader globalReader)
    {
        _console = console;
        _writer = writer;
        _globalReader = globalReader;
    }

    /// <summary>
    /// Executes the modes command in interactive mode.
    /// Uses Space to toggle modes and Enter to confirm changes.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> ExecuteInteractiveAsync(CancellationToken cancellationToken = default)
    {
        var resources = await _globalReader.GetSharedResourcesAsync(cancellationToken);

        if (resources.Resources.Count == 0)
        {
            _console.MarkupLine("[yellow]No resources configured.[/]");
            return 0;
        }

        var resourceIds = resources.Resources.Keys.ToList();
        var modes = resources.Resources.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Mode);
        var originalModes = new Dictionary<string, Mode>(modes);
        var selectedIndex = 0;
        var confirmed = false;

        await _console.Live(BuildRenderable(resourceIds, modes, selectedIndex))
            .AutoClear(true)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ctx.UpdateTarget(BuildRenderable(resourceIds, modes, selectedIndex));
                    ctx.Refresh();

                    var keyInfo = await _console.Input.ReadKeyAsync(true, cancellationToken);
                    if (keyInfo is null) break;

                    switch (keyInfo.Value.Key)
                    {
                        case ConsoleKey.UpArrow:
                            if (selectedIndex > 0) selectedIndex--;
                            break;
                        case ConsoleKey.DownArrow:
                            if (selectedIndex < resourceIds.Count - 1) selectedIndex++;
                            break;
                        case ConsoleKey.Spacebar:
                            modes[resourceIds[selectedIndex]] = ToggleMode(modes[resourceIds[selectedIndex]]);
                            break;
                        case ConsoleKey.Enter:
                            confirmed = true;
                            return;
                        case ConsoleKey.Escape:
                            return;
                    }
                }
            });

        if (!confirmed)
        {
            _console.MarkupLine("[yellow]No changes saved.[/]");
            return 0;
        }

        var modifiedIds = modes
            .Where(kvp => kvp.Value != originalModes[kvp.Key])
            .Select(kvp => kvp.Key)
            .ToList();

        if (modifiedIds.Count == 0)
        {
            _console.MarkupLine("[yellow]No changes made.[/]");
            return 0;
        }

        var updatedResources = resources;
        foreach (var id in modifiedIds)
        {
            updatedResources = updatedResources.UpdateResource(id, resources.Resources[id].WithMode(modes[id]));
        }

        await _writer.SaveGlobalAsync(updatedResources, cancellationToken);

        foreach (var id in modifiedIds)
        {
            _console.MarkupLine($"[green]Toggled '{id}' from {originalModes[id]} to {modes[id]}.[/]");
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
        var resources = await _globalReader.GetSharedResourcesAsync(cancellationToken);

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

    private static Rows BuildRenderable(List<string> resourceIds, Dictionary<string, Mode> modes, int selectedIndex)
    {
        var rows = new List<Markup>
        {
            new("[bold]Toggle modes[/] [dim](Space=toggle, Enter=confirm, Esc=cancel)[/]"),
            new("")
        };

        for (var i = 0; i < resourceIds.Count; i++)
        {
            var id = resourceIds[i];
            var mode = modes[id];
            rows.Add(i == selectedIndex
                ? new Markup($"> [blue bold][[{mode}]][/] {Markup.Escape(id)}")
                : new Markup($"  [[{mode}]] {Markup.Escape(id)}"));
        }

        return new Rows(rows);
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
