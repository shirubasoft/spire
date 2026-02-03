# Resource List Command

## Command

```
spire resource list
```

## Expected Behavior

- Outputs a JSON array of all shared resources from global configuration
- Always outputs JSON (remove `--json` flag as it's redundant)
- Empty configuration returns empty JSON array `[]`
- Reads from `~/.aspire/spire/aspire-shared-resources.json`

### Example Output

```json
{
  "resources": {
    "postgres": {
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
  }
}
```

### Exit Codes

- `0` - Success (including empty list)

## Tests

### Unit Tests

#### SharedResourcesConfigurationExtensionsTests

| Test | Description |
|------|-------------|
| `GetSharedResources_WhenFileDoesNotExist_ReturnsEmpty` | Returns `GlobalSharedResources.Empty` when config file missing |
| `GetSharedResources_WhenFileIsEmpty_ReturnsEmpty` | Returns empty when JSON file is empty or `{}` |
| `GetSharedResources_WhenFileHasResources_ReturnsAll` | Deserializes all resources correctly |
| `GetSharedResources_WhenFileHasInvalidJson_ThrowsException` | Throws on malformed JSON |

#### ResourceListCommandTests

| Test | Description |
|------|-------------|
| `Execute_WhenNoResources_ReturnsEmptyJsonArray` | Outputs `[]` |
| `Execute_WhenResourcesExist_ReturnsJsonArray` | Outputs all resources as JSON |
| `Execute_OutputIsValidJson` | Verifies output can be parsed as JSON |

### Integration Tests

#### ResourceListIntegrationTests

| Test | Description |
|------|-------------|
| `List_WithEmptyConfig_OutputsEmptyArray` | End-to-end with no config file |
| `List_WithPopulatedConfig_OutputsAllResources` | End-to-end with multiple resources |
| `List_JsonOutputMatchesConfigFile` | Round-trip verification |

## Source Generation

No source generation required for this command.

## Missing Contracts/Interfaces

None - extract logic to service class that accepts `TextWriter` for testability per system-commandline-reference.md.

## Implementation Tasks

1. [ ] Remove `--json` option from `ResourceListCommand`
2. [ ] Create service class that accepts `TextWriter` for testable output
3. [ ] Implement command handler using `SetHandler`
4. [ ] Add unit tests for `SharedResourcesConfigurationExtensions`
5. [ ] Add unit tests for `ResourceListCommand`
6. [ ] Add integration tests

## Definition of Done

- [ ] Command outputs JSON array of all resources
- [ ] Empty config returns `[]`
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No compiler warnings
- [ ] Code review approved
