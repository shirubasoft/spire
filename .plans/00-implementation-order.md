# Command Implementation Order

## Rationale

Commands are ordered by dependency and complexity:

1. Read operations first (list, info) - establish patterns
2. Write operations (import, generate) - populate configuration
3. Modification operations (remove, clear) - modify existing config
4. Specialized operations (build-image, modes)

## Implementation Order

| Order | Command | File | Status | Dependencies |
|-------|---------|------|--------|--------------|
| 1 | `resource list` | [01-resource-list.md](./01-resource-list.md) | Pending | None |
| 2 | `resource info` | [02-resource-info.md](./02-resource-info.md) | Pending | list patterns |
| 3 | `resource import` | [03-resource-import.md](./03-resource-import.md) | Pending | IGitService |
| 4 | `resource generate` | [04-resource-generate.md](./04-resource-generate.md) | Pending | import patterns |
| 5 | `resource remove` | [05-resource-remove.md](./05-resource-remove.md) | Pending | list, info |
| 6 | `resource clear` | [06-resource-clear.md](./06-resource-clear.md) | Pending | remove patterns |
| 7 | `resource build-image` | [07-resource-build-image.md](./07-resource-build-image.md) | Pending | IContainerImageService |
| 8 | `modes` | [08-modes.md](./08-modes.md) | Pending | Spectre.Console |
| 9 | `override` | [09-override.md](./09-override.md) | **DEFERRED** | TBD |

## Shared Infrastructure (Implement First)

Before implementing commands, these interfaces and implementations are needed:

### Console Output

See [system-commandline-reference.md](./system-commandline-reference.md) for API details.

- Use `InvocationConfiguration` with `TextWriter` for testable output
- Use `IAnsiConsole` (Spectre.Console) for interactive prompts and rich output
  - Inject via constructor for testability
  - Use `TestConsole` from `Spectre.Console.Testing` in unit tests

### Configuration

- [ ] `ISharedResourcesWriter` implementation
- [ ] `IRepositorySharedResourcesReader` - Read .aspire/settings.json

### Git

- [ ] `IGitService` implementation (see `Services/Git/IGitService.cs`)
  - `CloneRepositoryAsync` - Clone external repos
  - `GetRepositoryAsync` - Returns `GitRepository` with branch, commit, dirty state
  - `IsRepositoryCloned` - Check if repo exists at path
- [ ] Add `RootPath` property to `GitRepository` if needed for path resolution

### Containers

- [ ] `IContainerImageService` implementation
  - `ImageExistsAsync`
  - `BuildImageAsync`
  - `TagExistsAsync`
  - `TagImageAsync`

### Analysis

- [ ] `IProjectAnalyzer` - Analyze .csproj files
- [ ] `IDockerfileAnalyzer` - Analyze Dockerfile directories
- [ ] `IGitSettingsDetector` - Extract git remote/branch info
- [ ] `IBranchNameSanitizer` - Sanitize branch names for image tags

### Process

- [ ] `ICommandRunner` - Execute shell commands

## Core Schema Reference

See `src/core/` for type definitions:

- `Mode.cs` - Execution mode enum (Container, Project)
- `SharedResource.cs` - Resource with mode and settings
- `ContainerModeSettings.cs` - Container image configuration
- `ProjectModeSettings.cs` - Project directory configuration
- `GitRepositorySettings.cs` - Git repository URL and branch
- `GlobalSharedResources.cs` - Global config with Resources dictionary
- `RepositorySharedResources.cs` - Repo config with Resources and ExternalResources
- `ExternalResource.cs` - External git repo reference

JSON schemas: `schemas/shared-resources.schema.json`

## Model Changes (Methods to Add)

### GlobalSharedResources

- `TryGetResource(string id, out SharedResource? resource)` → bool
- `GetResource(string id)` → SharedResource?
- `RemoveResource(string id)` → GlobalSharedResources
- `ClearResources(IEnumerable<string>? ids = null)` → GlobalSharedResources
- `UpdateResource(string id, SharedResource resource)` → GlobalSharedResources
- `ContainsResource(string id)` → bool
- `Count` → int

### RepositorySharedResources

- `RemoveResource(string id)` → RepositorySharedResources
- `ClearResources(IEnumerable<string>? ids = null)` → RepositorySharedResources
- `ContainsResource(string id)` → bool
- `Count` → int

### SharedResource

- `WithMode(Mode mode)` → SharedResource

## NuGet Packages to Add

| Package | Project | Purpose |
|---------|---------|---------|
| `Spectre.Console` | CLI | Interactive terminal UI, `IAnsiConsole` |
| `Spectre.Console.Testing` | CLI.Tests | `TestConsole` for unit testing |
| `NSubstitute` | CLI.Tests | Mocking framework for service interfaces |

## Test Infrastructure

### Mocking

Use **NSubstitute** for all service mocks. Example:

```csharp
var writer = Substitute.For<ISharedResourcesWriter>();
writer.WriteAsync(Arg.Any<GlobalSharedResources>()).Returns(Task.CompletedTask);
```

### Console Testing

Use `TestConsole` from `Spectre.Console.Testing` for interactive prompt testing:

```csharp
var console = new TestConsole();
console.Interactive();
console.Input.PushTextWithEnter("y"); // Simulate user input
// Assert against console.Output
```

### Fixtures

- Integration test helpers for creating temp git repos
- Test fixtures for sample .aspire/settings.json files
