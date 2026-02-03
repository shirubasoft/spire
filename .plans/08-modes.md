# Modes Command

## Command

```
spire modes
```

## Expected Behavior

1. Display interactive select list of all global resources (using Spectre.Console)
2. Show current mode for each resource
3. Allow user to select a resource and flip its mode (Container ↔ Project)
4. Save the updated mode to global config
5. Just flips the Mode enum - keeps all existing settings intact

### Interactive Flow

```
Select a resource to toggle mode:

> [Container] postgres      ← Current mode shown
  [Project]   my-service
  [Container] redis

  Exit

───────────────────────────────

Toggled 'postgres' from Container to Project.

Select a resource to toggle mode:

  [Project]   postgres      ← Mode updated
> [Project]   my-service
  [Container] redis

  Exit
```

### Non-Interactive Mode

For scripting, support direct mode setting:

```
spire modes --id <resource-id> --mode <Container|Project>
```

### Options

| Option | Description |
|--------|-------------|
| `--id <id>` | Resource ID (for non-interactive use) |
| `--mode <mode>` | Target mode: Container or Project (for non-interactive use) |

### Exit Codes

- `0` - Success (or user selected Exit)
- `1` - Error (resource not found, write error)

## Tests

### Unit Tests

#### ModesCommandTests

| Test | Description |
|------|-------------|
| `Execute_Interactive_ShowsResourceList` | Displays all resources with modes |
| `Execute_Interactive_TogglesSelectedResource` | Mode flips on selection |
| `Execute_Interactive_SavesChanges` | Changes persisted to global config |
| `Execute_Interactive_ExitDoesNotSave` | Exit without changes works |
| `Execute_NonInteractive_SetsMode` | --id --mode works |
| `Execute_NonInteractive_InvalidId_ReturnsError` | Error on unknown ID |
| `Execute_NonInteractive_InvalidMode_ReturnsError` | Error on invalid mode |

#### ModeToggleTests

| Test | Description |
|------|-------------|
| `Toggle_FromContainer_ReturnsProject` | Container → Project |
| `Toggle_FromProject_ReturnsContainer` | Project → Container |
| `Toggle_PreservesAllSettings` | Other properties unchanged |

#### SharedResourceTests

| Test | Description |
|------|-------------|
| `WithMode_ReturnsNewInstance` | Immutable mode change |
| `WithMode_SameMode_ReturnsSameValues` | Idempotent |

### Integration Tests

#### ModesIntegrationTests

| Test | Description |
|------|-------------|
| `Modes_Interactive_LoadsAllResources` | Resources displayed from config |
| `Modes_ToggleMode_PersistsChange` | Change saved to file |
| `Modes_NonInteractive_SetsMode` | CLI flags work end-to-end |
| `Modes_WhenNoResources_ShowsEmptyMessage` | Empty config handled |

## Source Generation

No source generation required for this command.

## Console Interaction

Use `IAnsiConsole` (Spectre.Console) directly for interactive selection:

```csharp
// Inject IAnsiConsole via constructor
public class ModesHandler(IAnsiConsole console, ...)
{
    public int Execute()
    {
        while (true)
        {
            var choices = resources.Select(r => $"[{r.Mode}] {r.Id}").ToList();
            choices.Add("Exit");

            var choice = console.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a resource to toggle mode:")
                    .AddChoices(choices));

            if (choice == "Exit")
                return 0;

            // Toggle mode logic...
            console.MarkupLine($"[green]Toggled mode[/]");
        }
    }
}
```

For testing, use `TestConsole` from `Spectre.Console.Testing`:

```csharp
var console = new TestConsole();
console.Interactive();
console.Input.PushKey(ConsoleKey.DownArrow); // Select second item
console.Input.PushKey(ConsoleKey.Enter);      // Confirm selection
console.Input.PushKey(ConsoleKey.DownArrow);  // Navigate to Exit
console.Input.PushKey(ConsoleKey.DownArrow);
console.Input.PushKey(ConsoleKey.Enter);      // Select Exit
```

## Dependencies

| Package | Purpose |
|---------|---------|
| `Spectre.Console` | Interactive terminal UI, `IAnsiConsole` |
| `Spectre.Console.Testing` | `TestConsole` for unit testing |

## Changes to Existing Types

### SharedResource

Add method:

```csharp
public SharedResource WithMode(Mode mode);
```

### GlobalSharedResources

Add method:

```csharp
public GlobalSharedResources UpdateResource(string id, SharedResource resource);
```

## Implementation Tasks

1. [ ] Add `Spectre.Console` NuGet package to CLI project
2. [ ] Add `Spectre.Console.Testing` NuGet package to CLI.Tests project
3. [ ] Add `WithMode` method to `SharedResource`
4. [ ] Add `UpdateResource` method to `GlobalSharedResources`
5. [ ] Add `--id` and `--mode` options for non-interactive use
6. [ ] Implement interactive selection loop using `IAnsiConsole.Prompt(SelectionPrompt<T>)`
7. [ ] Implement mode toggle logic
8. [ ] Save changes after each toggle
9. [ ] Implement command handler with `IAnsiConsole` injection
10. [ ] Add unit tests (using `TestConsole`)
11. [ ] Add integration tests

## Definition of Done

- [ ] Interactive selector shows all resources with current mode
- [ ] Selecting a resource toggles its mode
- [ ] Changes saved to global config immediately
- [ ] Exit option available
- [ ] Non-interactive mode works with --id --mode
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No compiler warnings
- [ ] Code review approved
