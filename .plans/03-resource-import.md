# Resource Import Command

## Command

```
spire resource import [--yes] [--force]
```

## Expected Behavior

1. Finds the current git repository root
2. Locates `.aspire/settings.json` (RepositorySharedResources) in the repo
3. For each `ExternalResource` defined:
   - Clone to parent directory of current git root (one level above)
   - Skip if already cloned
   - Recursively import from cloned repo
4. Import all resources to global config (`~/.aspire/spire/aspire-shared-resources.json`)
5. Convert relative paths to absolute paths during import
6. Skip existing resources unless `--force` is provided

### Clone Location Example

```
/home/user/projects/
├── my-app/                    ← current git repo (cwd)
│   └── .aspire/settings.json  ← defines external: github.com/org/shared
├── org-shared/                ← cloned here (sibling to my-app)
│   └── .aspire/settings.json
```

### Options

| Option | Description |
|--------|-------------|
| `--yes` | Skip confirmation prompts |
| `--force` | Overwrite existing resources with same ID |

### Output Example

```
Importing from /home/user/projects/my-app...
  Found 3 resources in .aspire/settings.json

Cloning external repository: github.com/org/shared...
  Cloning to /home/user/projects/org-shared
  Found 2 resources in .aspire/settings.json

Import Summary:
  Imported: 4
  Skipped (already exists): 1
  Total: 5
```

### Exit Codes

- `0` - Success (all resources imported or skipped)
- `1` - Error (git operations failed, invalid config, etc.)

## Tests

### Unit Tests

#### ResourceImportCommandTests

| Test | Description |
|------|-------------|
| `Execute_WhenNotInGitRepo_ReturnsError` | Error when not in git repository |
| `Execute_WhenNoSettingsFile_ReturnsError` | Error when .aspire/settings.json missing |
| `Execute_WhenSettingsFileEmpty_ImportsNothing` | Handles empty settings gracefully |
| `Execute_WithResources_ImportsToGlobal` | Basic import flow works |
| `Execute_WithExistingResource_SkipsWithoutForce` | Respects existing resources |
| `Execute_WithExistingResourceAndForce_Overwrites` | --force overwrites |
| `Execute_ConvertsRelativePathsToAbsolute` | Path resolution works |

#### ExternalResourceImportTests

| Test | Description |
|------|-------------|
| `Import_WithExternalResource_ClonesRepo` | Clones external repositories |
| `Import_WithExternalResourceAlreadyCloned_SkipsClone` | Doesn't re-clone existing |
| `Import_WithExternalResource_ImportsRecursively` | Recursive import works |
| `Import_WithCircularExternalRef_DetectsAndStops` | Prevents infinite recursion |
| `Import_ClonesNextToCurrentRepo` | Clone location is correct |

#### PathResolutionTests

| Test | Description |
|------|-------------|
| `ResolveRelativePath_FromRepoRoot_ReturnsAbsolute` | Relative → absolute conversion |
| `ResolveRelativePath_WithDotDot_ResolvesCorrectly` | Handles `../` paths |
| `ResolveRelativePath_AlreadyAbsolute_ReturnsUnchanged` | Idempotent for absolute |

### Integration Tests

#### ResourceImportIntegrationTests

| Test | Description |
|------|-------------|
| `Import_FromRealGitRepo_PopulatesGlobalConfig` | End-to-end with real git repo |
| `Import_WithExternalRepo_ClonesAndImports` | Full external resource flow |
| `Import_MultipleRuns_IsIdempotent` | Re-running doesn't duplicate |
| `Import_WithForce_OverwritesExisting` | --force behavior verified |
Git| `Import_WithCircularExternalRefs_ImportsSuccessfully` | Circular reference between external repos is detected, all resources are cloned and imported without errors |

## Source Generation

No source generation required for this command.

## Missing Contracts/Interfaces

| Contract | Location | Purpose |
|----------|----------|---------|
| `IRepositorySharedResourcesReader` | `Services/Configuration/` | Read .aspire/settings.json from repo |
| `IGitCliResolver` | `Services/Git/` | Resolve git CLI (gh → git fallback) |

Handler accepts `IAnsiConsole` for progress output and confirmations per system-commandline-reference.md (Spectre.Console Integration section).

## Changes to Existing Types

### IGitService

Add methods:

```csharp
Task<string> GetRepositoryRootAsync(string path, CancellationToken ct);
string GetParentDirectory(string repoRoot);
Task CloneAsync(string url, string targetDirectory, CancellationToken ct);
```

### Git CLI Selection

The git CLI is selected in this order:

1. `gh` (GitHub CLI, if available on PATH) - preferred for GitHub repos
2. `git` (fallback)

```csharp
public interface IGitCliResolver
{
    Task<string> ResolveAsync(CancellationToken ct); // Returns "gh" or "git"
}
```

Use **CliWrap** for all git command execution (not `System.Diagnostics.Process` directly).

### ISharedResourcesWriter

Already has `SaveGlobalAsync` - sufficient.

### RepositorySharedResources

Ensure `ExternalResources` property is correctly deserialized.

## Implementation Tasks

1. [ ] Add CliWrap NuGet package dependency
2. [ ] Create `IGitCliResolver` interface and implementation (gh → git fallback)
3. [ ] Create `IRepositorySharedResourcesReader` interface
4. [ ] Implement repository settings file reader
5. [ ] Add `GetRepositoryRootAsync` to `IGitService`
6. [ ] Add `CloneAsync` to `IGitService` using CliWrap
7. [ ] Implement external resource cloning logic
8. [ ] Implement recursive import with cycle detection
9. [ ] Implement path resolution (relative → absolute)
10. [ ] Implement command handler with `IAnsiConsole` for progress output
11. [ ] Add unit tests
12. [ ] Add integration tests with test git repos

## Definition of Done

- [ ] Command finds and reads .aspire/settings.json from current git repo
- [ ] External resources are cloned to parent directory
- [ ] Already-cloned repos are reused
- [ ] Resources are imported to global config
- [ ] Relative paths converted to absolute
- [ ] Existing resources skipped unless --force
- [ ] Progress output during import
- [ ] Summary output at end
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No compiler warnings
- [ ] Code review approved
