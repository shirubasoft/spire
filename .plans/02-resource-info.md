# Resource Info Command

## Command

```
spire resource info --id <resource-id>
```

## Expected Behavior

- Outputs a single shared resource as JSON object
- Requires `--id` option (already defined as required)
- When resource not found: error message to stderr, exit code 1
- Reads from `~/.aspire/spire/aspire-shared-resources.json`

### Example Output (Success)

```json
{
  "mode": "Container",
  "containerMode": {
    "imageName": "postgres",
    "imageRegistry": "docker.io",
    "imageTag": "latest",
    "buildCommand": "docker build -t postgres .",
    "buildWorkingDirectory": "/home/user/projects/postgres"
  },
  "projectMode": null,
  "gitRepository": {
    "url": "https://github.com/org/repo",
    "defaultBranch": "main"
  }
}
```

### Example Output (Not Found)

```
Error: Resource 'unknown-id' not found.
```

### Exit Codes

- `0` - Success, resource found
- `1` - Resource not found

## Tests

### Unit Tests

#### ResourceInfoCommandTests

| Test | Description |
|------|-------------|
| `Execute_WhenResourceExists_ReturnsJsonObject` | Outputs single resource as JSON |
| `Execute_WhenResourceNotFound_WritesErrorToStderr` | Error message to stderr |
| `Execute_WhenResourceNotFound_ReturnsExitCode1` | Non-zero exit code |
| `Execute_OutputIsValidJson` | Verifies output can be parsed |
| `Execute_WhenIdIsEmpty_ReturnsValidationError` | Validates required --id |

#### GlobalSharedResourcesTests

| Test | Description |
|------|-------------|
| `TryGetResource_WhenExists_ReturnsTrue` | Lookup by ID succeeds |
| `TryGetResource_WhenNotExists_ReturnsFalse` | Lookup by ID fails gracefully |
| `Indexer_WhenExists_ReturnsResource` | Dictionary-style access works |
| `Indexer_WhenNotExists_ThrowsKeyNotFound` | Throws on missing key |

### Integration Tests

#### ResourceInfoIntegrationTests

| Test | Description |
|------|-------------|
| `Info_WithExistingResource_OutputsJson` | End-to-end success case |
| `Info_WithNonExistingResource_ExitsWithError` | End-to-end failure case |
| `Info_JsonMatchesResourceInConfig` | Output matches source data |

## Source Generation

No source generation required for this command.

## Missing Contracts/Interfaces

None - extract logic to service class that accepts `TextWriter` for testability per system-commandline-reference.md.

## Changes to Existing Types

### GlobalSharedResources

Add lookup methods:

```csharp
public bool TryGetResource(string id, out SharedResource? resource);
public SharedResource? GetResource(string id);
```

## Implementation Tasks

1. [ ] Add `TryGetResource` / `GetResource` methods to `GlobalSharedResources`
2. [ ] Create `IConsoleOutput` interface (if not done in list)
3. [ ] Implement command handler using `SetHandler`
4. [ ] Write error to stderr on not found
5. [ ] Return exit code 1 on not found
6. [ ] Add unit tests
7. [ ] Add integration tests

## Definition of Done

- [ ] Command outputs JSON object for existing resource
- [ ] Error message to stderr when not found
- [ ] Exit code 1 when not found
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No compiler warnings
- [ ] Code review approved
