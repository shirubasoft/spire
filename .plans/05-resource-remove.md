# Resource Remove Command

## Command

```
spire resource remove --id <resource-id> [--yes]
```

## Expected Behavior

1. Removes resource from global config (`~/.aspire/spire/aspire-shared-resources.json`)
2. Also removes from repository settings (`.aspire/settings.json`) if present
3. Prompts for confirmation unless `--yes` provided
4. Error if resource doesn't exist in global config

### Options

| Option | Description |
|--------|-------------|
| `--id <id>` | Resource identifier to remove (required) |
| `--yes` | Skip confirmation prompt |

### Example Output

```
Remove resource 'postgres'?

This will remove from:
  - ~/.aspire/spire/aspire-shared-resources.json
  - /home/user/projects/my-app/.aspire/settings.json

Continue? [y/N] y

Removed 'postgres' from global config.
Removed 'postgres' from repository settings.
```

### Example Output (Not Found)

```
Error: Resource 'unknown-id' not found.
```

### Exit Codes

- `0` - Success (resource removed)
- `1` - Error (resource not found, file write error)

## Tests

### Unit Tests

#### ResourceRemoveCommandTests

| Test | Description |
|------|-------------|
| `Execute_WhenResourceExists_RemovesFromGlobal` | Removes from global config |
| `Execute_WhenResourceExistsInRepo_RemovesFromBoth` | Removes from both locations |
| `Execute_WhenResourceNotFound_ReturnsError` | Error on missing resource |
| `Execute_WithoutYes_PromptsConfirmation` | Confirmation prompt works |
| `Execute_WithYes_SkipsConfirmation` | --yes skips prompt |
| `Execute_WhenUserDeclinesPrompt_DoesNotRemove` | Respects user decline |

#### GlobalSharedResourcesTests

| Test | Description |
|------|-------------|
| `RemoveResource_WhenExists_ReturnsNewInstanceWithout` | Immutable removal |
| `RemoveResource_WhenNotExists_ThrowsException` | Error on missing |
| `RemoveResource_PreservesOtherResources` | Doesn't affect others |

#### RepositorySharedResourcesTests

| Test | Description |
|------|-------------|
| `RemoveResource_WhenExists_ReturnsNewInstanceWithout` | Immutable removal |
| `RemoveResource_WhenNotExists_ReturnsUnchanged` | No error if missing |

#### JsonStateValidationTests

| Test | Description |
|------|-------------|
| `Remove_GlobalConfig_BeforeStateHasResource` | Verify resource exists in JSON before removal |
| `Remove_GlobalConfig_AfterStateWithoutResource` | Verify resource absent in JSON after removal |
| `Remove_GlobalConfig_OtherResourcesUnchanged` | Other resources remain identical in JSON |
| `Remove_GlobalConfig_JsonStructureRemainsValid` | JSON file is well-formed after removal |
| `Remove_RepositorySettings_BeforeStateHasResource` | Verify resource exists in repo JSON before removal |
| `Remove_RepositorySettings_AfterStateWithoutResource` | Verify resource absent in repo JSON after removal |
| `Remove_RepositorySettings_OtherResourcesUnchanged` | Other resources remain identical in repo JSON |
| `Remove_RepositorySettings_JsonStructureRemainsValid` | Repo JSON file is well-formed after removal |
| `Remove_LastResource_ResultsInEmptyResourcesObject` | Removing last resource leaves empty `{}` not null |

### Integration Tests

#### ResourceRemoveIntegrationTests

| Test | Description |
|------|-------------|
| `Remove_ExistingResource_RemovesFromGlobal` | End-to-end global removal |
| `Remove_ResourceInBothLocations_RemovesBoth` | Both files updated |
| `Remove_ResourceOnlyInGlobal_RemovesGlobalOnly` | Handles repo-missing case |
| `Remove_NonExistingResource_ReturnsError` | Error case end-to-end |
| `Remove_WithYes_NoPrompt` | --yes flag works |
| `Remove_VerifyJsonStateBeforeAndAfter_GlobalConfig` | Read JSON before, remove, read JSON after, compare |
| `Remove_VerifyJsonStateBeforeAndAfter_RepositorySettings` | Read repo JSON before, remove, read after, compare |
| `Remove_MultipleResources_PreservesOthersInJson` | JSON contains 3 resources, remove 1, verify 2 remain intact |

## Source Generation

No source generation required for this command.

## Console Interaction

Use `IAnsiConsole` (Spectre.Console) directly for confirmation prompts:

```csharp
// Inject IAnsiConsole via constructor
public class ResourceRemoveHandler(IAnsiConsole console, ...)
{
    public int Execute(string id, bool yes)
    {
        if (!yes && !console.Confirm($"Remove resource '{id}'?", defaultValue: false))
            return 0;

        // ... removal logic
        console.MarkupLine($"[green]Removed '{id}'[/]");
        return 0;
    }
}
```

For testing, use `TestConsole` from `Spectre.Console.Testing`:

```csharp
var console = new TestConsole();
console.Interactive();
console.Input.PushTextWithEnter("y"); // Simulate user confirmation
```

## Changes to Existing Types

### GlobalSharedResources

Add method:

```csharp
public GlobalSharedResources RemoveResource(string id);
public bool ContainsResource(string id);
```

### RepositorySharedResources

Add method:

```csharp
public RepositorySharedResources RemoveResource(string id);
public bool ContainsResource(string id);
```

### ISharedResourcesWriter

Already has needed methods.

## Implementation Tasks

1. [ ] Add `--id` option to command (using existing `ResourceOptions.Id`)
2. [ ] Add `RemoveResource` method to `GlobalSharedResources`
3. [ ] Add `RemoveResource` method to `RepositorySharedResources`
4. [ ] Add `ContainsResource` method to both types
5. [ ] Implement confirmation prompt using `IAnsiConsole.Confirm()`
6. [ ] Implement removal from both locations
7. [ ] Detect if in git repo for repo settings removal
8. [ ] Implement command handler with `IAnsiConsole` injection
9. [ ] Add unit tests (using `TestConsole`)
10. [ ] Add integration tests

## Definition of Done

- [ ] Command removes resource from global config
- [ ] Command removes from repository settings if present
- [ ] Confirmation prompt shown unless --yes
- [ ] Error when resource not found
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No compiler warnings
- [ ] Code review approved
