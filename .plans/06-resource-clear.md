# Resource Clear Command

## Command

```
spire resource clear [--ids <id1> <id2> ...] [--include-repo] [--yes]
```

## Expected Behavior

1. By default, clears **all** resources from global config
2. With `--ids`, clears only specified resources
3. Only affects global config unless `--include-repo` is provided
4. Prompts for confirmation unless `--yes` provided

### Options

| Option | Description |
|--------|-------------|
| `--ids <id1> <id2> ...` | Specific resource IDs to clear (optional, clears all if omitted) |
| `--include-repo` | Also clear from repository settings (`.aspire/settings.json`) |
| `--yes` | Skip confirmation prompt |

### Example Output (Clear All)

```
Clear all 5 resources from global config?

Continue? [y/N] y

Cleared 5 resources from global config.
```

### Example Output (Clear Specific)

```
Clear resources: postgres, redis?

This will remove from:
  - ~/.aspire/spire/aspire-shared-resources.json

Continue? [y/N] y

Cleared 2 resources from global config.
```

### Example Output (Include Repo)

```
Clear all resources from global config and repository settings?

This will remove from:
  - ~/.aspire/spire/aspire-shared-resources.json
  - /home/user/projects/my-app/.aspire/settings.json

Continue? [y/N] y

Cleared 5 resources from global config.
Cleared 3 resources from repository settings.
```

### Exit Codes

- `0` - Success
- `1` - Error (file write error, specified ID not found)

## Tests

### Unit Tests

#### ResourceClearCommandTests

| Test | Description |
|------|-------------|
| `Execute_WithoutIds_ClearsAllFromGlobal` | Clears all resources |
| `Execute_WithIds_ClearsOnlySpecified` | Clears subset |
| `Execute_WithIncludeRepo_ClearsBothLocations` | Repo settings cleared |
| `Execute_WithoutIncludeRepo_GlobalOnly` | Repo settings untouched |
| `Execute_WithInvalidId_ReturnsError` | Error on unknown ID |
| `Execute_WithoutYes_PromptsConfirmation` | Confirmation shown |
| `Execute_WithYes_SkipsConfirmation` | --yes skips prompt |
| `Execute_WhenUserDeclinesPrompt_DoesNotClear` | Respects decline |
| `Execute_WhenAlreadyEmpty_Succeeds` | Idempotent |

#### GlobalSharedResourcesTests

| Test | Description |
|------|-------------|
| `Clear_ReturnsEmptyInstance` | Complete clear works |
| `ClearResources_WithIds_ReturnsWithoutSpecified` | Partial clear |
| `ClearResources_PreservesUnspecifiedResources` | Others untouched |

#### JsonStateValidationTests

| Test | Description |
|------|-------------|
| `ClearAll_GlobalConfig_BeforeStateHasResources` | Verify resources exist in JSON before clear |
| `ClearAll_GlobalConfig_AfterStateIsEmpty` | Verify JSON has empty resources object after clear all |
| `ClearAll_GlobalConfig_JsonStructureRemainsValid` | JSON file is well-formed after clear |
| `ClearSpecific_GlobalConfig_BeforeStateHasAllResources` | Verify all resources exist before partial clear |
| `ClearSpecific_GlobalConfig_AfterStateWithoutCleared` | Specified resources absent after clear |
| `ClearSpecific_GlobalConfig_UnspecifiedResourcesUnchanged` | Unspecified resources remain identical |
| `ClearAll_RepositorySettings_BeforeStateHasResources` | Verify resources exist in repo JSON before |
| `ClearAll_RepositorySettings_AfterStateIsEmpty` | Repo JSON has empty resources after clear |
| `ClearAll_RepositorySettings_JsonStructureRemainsValid` | Repo JSON well-formed after clear |
| `ClearSpecific_RepositorySettings_UnspecifiedResourcesUnchanged` | Unspecified resources intact in repo |
| `Clear_ResultsInEmptyResourcesObject_NotNull` | Clearing all leaves `"resources": {}` not null |

### Integration Tests

#### ResourceClearIntegrationTests

| Test | Description |
|------|-------------|
| `Clear_All_EmptiesGlobalConfig` | End-to-end clear all |
| `Clear_SpecificIds_RemovesOnlyThose` | End-to-end partial |
| `Clear_WithIncludeRepo_ClearsBoth` | Both locations cleared |
| `Clear_WithoutIncludeRepo_PreservesRepo` | Repo preserved |
| `Clear_AlreadyEmpty_Succeeds` | Idempotent behavior |
| `Clear_WithYes_NoPrompt` | --yes flag works |
| `ClearAll_VerifyJsonStateBeforeAndAfter_GlobalConfig` | Read JSON before, clear, read JSON after, verify empty |
| `ClearAll_VerifyJsonStateBeforeAndAfter_RepositorySettings` | Read repo JSON before, clear, read after, verify empty |
| `ClearSpecific_VerifyJsonStateBeforeAndAfter` | JSON contains 5 resources, clear 2, verify 3 remain intact |
| `Clear_WithIncludeRepo_VerifyBothJsonFilesCleared` | Verify both JSON files have empty resources |

## Source Generation

No source generation required for this command.

## Console Interaction

Use `IAnsiConsole` (Spectre.Console) directly for confirmation prompts:

```csharp
// Inject IAnsiConsole via constructor
public class ResourceClearHandler(IAnsiConsole console, ...)
{
    public int Execute(string[]? ids, bool includeRepo, bool yes)
    {
        var message = ids is null
            ? $"Clear all {count} resources?"
            : $"Clear resources: {string.Join(", ", ids)}?";

        if (!yes && !console.Confirm(message, defaultValue: false))
            return 0;

        // ... clear logic
        console.MarkupLine($"[green]Cleared {count} resources[/]");
        return 0;
    }
}
```

For testing, use `TestConsole` from `Spectre.Console.Testing`.

## Changes to Existing Types

### GlobalSharedResources

Add methods:

```csharp
public static GlobalSharedResources Empty { get; } // Already exists
public GlobalSharedResources ClearResources(IEnumerable<string>? ids = null);
public int Count { get; }
```

### RepositorySharedResources

Add methods:

```csharp
public RepositorySharedResources ClearResources(IEnumerable<string>? ids = null);
public int Count { get; }
```

### ResourceOptions

Update `Ids` to be optional (currently required):

```csharp
public static readonly Option<string[]> Ids = new(name: "--ids")
{
    Required = false,  // Changed from true
    Arity = ArgumentArity.ZeroOrMore,
    Description = "Resource IDs to clear (clears all if omitted)"
};
```

## Implementation Tasks

1. [ ] Make `--ids` optional in `ResourceOptions`
2. [ ] Add `--include-repo` option to command
3. [ ] Add `ClearResources` method to `GlobalSharedResources`
4. [ ] Add `ClearResources` method to `RepositorySharedResources`
5. [ ] Add `Count` property to both types
6. [ ] Implement confirmation prompt using `IAnsiConsole.Confirm()`
7. [ ] Implement clear logic (all vs specific)
8. [ ] Implement repo settings clearing with flag
9. [ ] Implement command handler with `IAnsiConsole` injection
10. [ ] Add unit tests (using `TestConsole`)
11. [ ] Add integration tests

## Definition of Done

- [ ] Command clears all resources by default
- [ ] --ids allows clearing specific resources
- [ ] --include-repo clears repository settings too
- [ ] Confirmation prompt shown unless --yes
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No compiler warnings
- [ ] Code review approved
