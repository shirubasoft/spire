# Resource Build Image Command

## Command

```
spire resource build-image --ids <id1> <id2> ... [--force]
```

## Expected Behavior

1. Executes `buildCommand` from `containerModeSettings` for each specified resource
2. Tags the image with:
   - Git commit hash (short, e.g., `abc1234`)
   - Safe branch name (lowercase, slashes/invalid chars removed)
   - `latest`
3. **Skip build if commit hash tag already exists** (not branch or latest)
4. `--force` rebuilds even if commit hash tag exists
5. Build only - no push to registry

### Safe Branch Name Examples

| Original Branch | Safe Name |
|-----------------|-----------|
| `main` | `main` |
| `feature/add-auth` | `feature-add-auth` |
| `Feature/Add_Auth` | `feature-add-auth` |
| `release/v1.0.0` | `release-v1.0.0` |
| `hotfix/BUG-123` | `hotfix-bug-123` |

### Image Tags Generated

For resource `my-service` with `ContainerMode.ImageRegistry = "docker.io"` and `ContainerMode.ImageName = "my-service"`, on branch `feature/auth` at commit `abc1234`:

```
docker.io/my-service:abc1234
docker.io/my-service:feature-auth
docker.io/my-service:latest
```

Note: The `ContainerMode.ImageTag` property stores the "safe branch name" tag that represents the current build configuration.

### Options

| Option | Description |
|--------|-------------|
| `--ids <id1> <id2> ...` | Resource IDs to build (required) |
| `--force` | Rebuild even if commit hash tag exists |

### Example Output

```
Building images for: my-service, postgres

[my-service]
  Branch: feature/auth
  Commit: abc1234
  ContainerMode.ImageRegistry: docker.io
  ContainerMode.ImageName: my-service
  Checking existing tags...
  Tag abc1234 not found, building...
  Running: dotnet publish --os linux --arch x64 /t:PublishContainer
  Build completed.
  Tags applied: abc1234, feature-auth, latest
  Updated ContainerMode.ImageTag: feature-auth

[postgres]
  Branch: main
  Commit: def5678
  ContainerMode.ImageRegistry: docker.io
  ContainerMode.ImageName: postgres
  Checking existing tags...
  Tag def5678 exists, skipping (use --force to rebuild)

Summary:
  Built: 1
  Skipped: 1
```

### Exit Codes

- `0` - Success (all builds completed or skipped)
- `1` - Error (build failed, resource not found, no containerModeSettings)

## Tests

### Unit Tests

#### ResourceBuildImageCommandTests

| Test | Description |
|------|-------------|
| `Execute_WithValidIds_BuildsImages` | Runs buildCommand for each |
| `Execute_WhenResourceNotFound_ReturnsError` | Error on unknown ID |
| `Execute_WhenNoContainerSettings_ReturnsError` | Error if mode is Project-only |
| `Execute_WhenNoBuildCommand_ReturnsError` | Error if buildCommand is null |
| `Execute_WhenCommitTagExists_Skips` | Skip existing commit tag |
| `Execute_WhenCommitTagExistsWithForce_Rebuilds` | --force rebuilds |
| `Execute_WhenBranchTagExists_StillBuilds` | Branch tag doesn't skip |

#### SafeBranchNameTests

| Test | Description |
|------|-------------|
| `Sanitize_SimpleBranch_Unchanged` | `main` → `main` |
| `Sanitize_WithSlash_ReplacesWithDash` | `feature/x` → `feature-x` |
| `Sanitize_Uppercase_Lowercases` | `Feature` → `feature` |
| `Sanitize_WithUnderscore_ReplacesWithDash` | `add_feature` → `add-feature` |
| `Sanitize_MultipleInvalid_ReplacesAll` | Complex cases handled |
| `Sanitize_LeadingTrailingDash_Trimmed` | No leading/trailing dashes |

#### ImageTagGeneratorTests

| Test | Description |
|------|-------------|
| `Generate_ReturnsThreeTags` | Commit, branch, latest |
| `Generate_CommitIsShortHash` | 7 characters |
| `Generate_BranchIsSanitized` | Safe name used |
| `Generate_LatestIsLiteral` | "latest" exactly |

### Integration Tests

#### ResourceBuildImageIntegrationTests

| Test | Description |
|------|-------------|
| `Build_WithDotnetProject_RunsPublishCommand` | Real dotnet publish |
| `Build_WithDockerfile_RunsDockerBuild` | Real docker build |
| `Build_WhenImageExists_Skips` | Skip logic works |
| `Build_WithForce_Rebuilds` | --force overrides skip |
| `Build_AppliesAllTags` | All three tags applied |

## Source Generation

No source generation required for this command.

## Missing Contracts/Interfaces

| Contract | Location | Purpose |
|----------|----------|---------|
| `IBranchNameSanitizer` | `Services/Git/` | Sanitize branch names for tags |
| `IImageTagGenerator` | `Services/Containers/` | Generate image tags |
| `IContainerRuntimeResolver` | `Services/Containers/` | Resolve container runtime (env → docker → podman) |
| `ICommandRunner` | `Services/Process/` | Execute build commands |

### Container Runtime Selection

The container runtime is selected in this order:
1. `$ASPIRE_CONTAINER_RUNTIME` environment variable (if set)
2. `docker` (if available on PATH)
3. `podman` (fallback)

```csharp
public interface IContainerRuntimeResolver
{
    Task<string> ResolveAsync(CancellationToken ct); // Returns "docker" or "podman"
}
```

### Process Execution

Use **CliWrap** for all process/command execution (not `System.Diagnostics.Process` directly).

```csharp
// ICommandRunner implementation using CliWrap
public class CommandRunner : ICommandRunner
{
    public async Task<CommandResult> RunAsync(
        string command,
        string arguments,
        string workingDirectory,
        CancellationToken ct)
    {
        var result = await Cli.Wrap(command)
            .WithArguments(arguments)
            .WithWorkingDirectory(workingDirectory)
            .ExecuteBufferedAsync(ct);

        return new CommandResult(result.ExitCode, result.StandardOutput, result.StandardError);
    }
}
```

Benefits:
- Clean async API
- Proper cancellation support
- Built-in output buffering
- No manual process cleanup

## Changes to Existing Types

### IContainerImageService

Add methods:

```csharp
Task<bool> TagExistsAsync(string registry, string imageName, string tag, CancellationToken ct);
Task TagImageAsync(string imageName, IEnumerable<string> tags, CancellationToken ct);
```

### IGitService

Use existing `GetRepositoryAsync` which returns `GitRepository` with:
- `CurrentBranch` - for safe branch name tag
- `LatestCommitHash` - for commit tag (truncate to 7 chars)

## Implementation Tasks

1. [ ] Add CliWrap NuGet package dependency (if not already added)
2. [ ] Create `IContainerRuntimeResolver` interface and implementation (env → docker → podman)
3. [ ] Create `IBranchNameSanitizer` interface and implementation
4. [ ] Create `IImageTagGenerator` interface and implementation
5. [ ] Create `ICommandRunner` interface and CliWrap-based implementation
6. [ ] Add `TagExistsAsync` to `IContainerImageService`
7. [ ] Add git branch/commit methods to `IGitService`
8. [ ] Implement skip logic based on commit tag existence
9. [ ] Implement tagging with all three tags
10. [ ] Implement progress output during build using `IAnsiConsole`
11. [ ] Implement command handler
12. [ ] Add unit tests
13. [ ] Add integration tests

## Definition of Done

- [ ] Command executes buildCommand for specified resources
- [ ] Images tagged with commit hash, safe branch name, latest
- [ ] Skip build if commit tag exists (not branch/latest)
- [ ] --force overrides skip behavior
- [ ] Error if resource lacks containerModeSettings or buildCommand
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No compiler warnings
- [ ] Code review approved
