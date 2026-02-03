# Resource Generate Command

## Command

```
spire resource generate <path-to-project-or-dockerfile> [--id <id>] [--yes]
```

## Expected Behavior

1. Takes a path to a .csproj file, Dockerfile, or directory
2. **Smart path resolution** (if directory is provided):
   - First, look for a `.csproj` file in the directory
   - If no `.csproj` found, look for a `Dockerfile`
   - Error if neither is found
3. Auto-detects resource settings with smart defaults
4. Prompts for resource ID (unless `--id` provided)
5. For .csproj files: configures **both** project and container modes
6. Auto-detects git repository settings from current repo
7. Saves to both:
   - Repository settings (`.aspire/settings.json`)
   - Global config (`~/.aspire/spire/aspire-shared-resources.json`)

### Mode Detection

| Input | Resolution | Project Mode | Container Mode |
|-------|------------|--------------|----------------|
| `.csproj` file | Direct | Yes (project directory) | Yes (dotnet publish container) |
| `Dockerfile` | Direct | No | Yes (docker build) |
| Directory with `.csproj` | Auto-detect `.csproj` | Yes (project directory) | Yes (dotnet publish container) |
| Directory with `Dockerfile` only | Auto-detect `Dockerfile` | No | Yes (docker build) |
| Directory with both | Prefers `.csproj` | Yes (project directory) | Yes (dotnet publish container) |

### Container Build Command (for .csproj)

```bash
dotnet publish --os linux --arch x64 /t:PublishContainer
```

### Options

| Option | Description |
|--------|-------------|
| `--id <id>` | Resource identifier (prompted if not provided) |
| `--yes` | Skip confirmation prompts |
| `--image-name <name>` | Override container image name (default: project name) |
| `--image-registry <registry>` | Override registry (default: docker.io) |

### Example Usage

```bash
# Generate from csproj file directly
spire resource generate ./src/MyService/MyService.csproj --id my-service

# Generate from directory (auto-detects .csproj)
spire resource generate ./src/MyService --id my-service

# Generate from Dockerfile directory (no .csproj found, falls back to Dockerfile)
spire resource generate ./docker/postgres/ --id postgres-db
```

### Example Output

```
Analyzing ./src/MyService/MyService.csproj...

Detected settings:
  ID: my-service
  Mode: Project + Container
  ProjectMode.ProjectDirectory: /home/user/projects/my-app/src/MyService
  ContainerMode.ImageName: my-service
  ContainerMode.ImageRegistry: docker.io
  ContainerMode.ImageTag: latest
  ContainerMode.BuildCommand: dotnet publish --os linux --arch x64 /t:PublishContainer
  ContainerMode.BuildWorkingDirectory: /home/user/projects/my-app/src/MyService
  GitRepository.Url: https://github.com/user/my-app
  GitRepository.DefaultBranch: main

Save to repository settings and global config? [Y/n] y

Saved to:
  - /home/user/projects/my-app/.aspire/settings.json
  - ~/.aspire/spire/aspire-shared-resources.json
```

### Generated Resource Structure

#### Global Config (`~/.aspire/spire/aspire-shared-resources.json`)

For a resource with ID `my-service`, the entry in `GlobalSharedResources.Resources`:

```json
{
  "my-service": {
    "mode": "Project",
    "containerMode": {
      "imageName": "my-service",
      "imageRegistry": "docker.io",
      "imageTag": "latest",
      "buildCommand": "dotnet publish --os linux --arch x64 /t:PublishContainer",
      "buildWorkingDirectory": "/home/user/projects/my-app/src/MyService"
    },
    "projectMode": {
      "projectDirectory": "/home/user/projects/my-app/src/MyService"
    },
    "gitRepository": {
      "url": "https://github.com/user/my-app",
      "defaultBranch": "main"
    }
  }
}
```

#### Repository Settings (`.aspire/settings.json`)

The repository settings use **relative paths** (relative to repo root):

```json
{
  "resources": {
    "my-service": {
      "mode": "Project",
      "containerMode": {
        "imageName": "my-service",
        "imageRegistry": "docker.io",
        "imageTag": "latest",
        "buildCommand": "dotnet publish --os linux --arch x64 /t:PublishContainer",
        "buildWorkingDirectory": "./src/MyService"
      },
      "projectMode": {
        "projectDirectory": "./src/MyService"
      }
    }
  }
}
```

**Key differences from global config:**
- Paths are relative to repository root (e.g., `./src/MyService`)
- No `gitRepository` section (implicit from current repo)
- Portable across machines when repo is cloned

### Exit Codes

- `0` - Success
- `1` - Error (invalid path, not a project/container, git errors)

## Tests

### Unit Tests

#### ResourceGenerateCommandTests

| Test | Description |
|------|-------------|
| `Execute_WithCsprojPath_ConfiguresBothModes` | .csproj gets project + container |
| `Execute_WithDockerfilePath_ConfiguresContainerOnly` | Dockerfile gets container only |
| `Execute_WithDirectoryContainingCsproj_AutoDetectsCsproj` | Directory with .csproj auto-resolves |
| `Execute_WithDirectoryContainingDockerfile_AutoDetectsDockerfile` | Directory with Dockerfile auto-resolves |
| `Execute_WithDirectoryContainingBoth_PrefersCsproj` | Directory with both prefers .csproj |
| `Execute_WithEmptyDirectory_ReturnsError` | Error when no .csproj or Dockerfile found |
| `Execute_WithInvalidPath_ReturnsError` | Error on non-existent path |
| `Execute_WithNonProjectPath_ReturnsError` | Error on invalid file type |
| `Execute_PromptsForIdWhenNotProvided` | Interactive ID prompt |
| `Execute_UsesProvidedId` | --id flag works |

#### RepositorySettingsGenerationTests

| Test | Description |
|------|-------------|
| `Generate_WritesRelativePathsToRepositorySettings` | Paths in repo settings are relative to repo root |
| `Generate_OmitsGitRepositoryFromRepositorySettings` | No gitRepository section in repo settings |
| `Generate_WritesAbsolutePathsToGlobalConfig` | Paths in global config are absolute |
| `Generate_IncludesGitRepositoryInGlobalConfig` | gitRepository section present in global config |
| `Generate_RepositorySettingsMatchesExpectedStructure` | Full structure validation for repo settings |
| `Generate_GlobalConfigMatchesExpectedStructure` | Full structure validation for global config |

#### ProjectAnalyzerTests

| Test | Description |
|------|-------------|
| `Analyze_CsprojFile_ExtractsProjectName` | Gets name from .csproj |
| `Analyze_CsprojFile_DeterminesProjectDirectory` | Correct directory path |
| `Analyze_CsprojFile_BuildsContainerCommand` | Correct dotnet publish command |
| `Analyze_DirectoryWithDockerfile_DetectsDockerfile` | Finds Dockerfile |
| `Analyze_DirectoryWithoutDockerfile_ReturnsNull` | Handles missing Dockerfile |

#### GitSettingsDetectorTests

| Test | Description |
|------|-------------|
| `Detect_InGitRepo_ReturnsSettings` | Extracts remote URL and branch |
| `Detect_NotInGitRepo_ReturnsNull` | Handles non-git directory |
| `Detect_WithMultipleRemotes_UsesOrigin` | Prefers 'origin' remote |
| `Detect_ParsesGitHubUrl_Correctly` | URL parsing works |
| `Detect_ParsesGitLabUrl_Correctly` | Multiple providers supported |

### Integration Tests

#### ResourceGenerateIntegrationTests

| Test | Description |
|------|-------------|
| `Generate_FromCsproj_SavesToBothLocations` | End-to-end .csproj flow |
| `Generate_FromDockerfile_SavesToBothLocations` | End-to-end Dockerfile flow |
| `Generate_WithGitRepo_IncludesGitSettings` | Git settings included in global only |
| `Generate_CreatesSettingsFileIfMissing` | Creates .aspire/settings.json |
| `Generate_AppendsToExistingSettings` | Doesn't overwrite existing |
| `Generate_RepositorySettings_ContainsRelativePaths` | Verify repo settings has relative paths |
| `Generate_RepositorySettings_OmitsGitRepository` | Verify repo settings has no git section |
| `Generate_GlobalConfig_ContainsAbsolutePaths` | Verify global config has absolute paths |

## Source Generation

No source generation required for this command.

## Missing Contracts/Interfaces

| Contract | Location | Purpose |
|----------|----------|---------|
| `IProjectAnalyzer` | `Services/Analysis/` | Analyze .csproj files |
| `IDockerfileAnalyzer` | `Services/Analysis/` | Analyze Dockerfile directories |
| `IGitSettingsDetector` | `Services/Git/` | Extract git remote/branch info |

### Console Interaction

Use `IAnsiConsole` (Spectre.Console) directly for interactive prompts:

```csharp
// Inject IAnsiConsole via constructor
public class ResourceGenerateHandler(IAnsiConsole console, ...)
{
    public int Execute(...)
    {
        var id = console.Ask<string>("Enter resource ID:");
        if (!console.Confirm("Save to config?"))
            return 0;
        // ...
    }
}
```

For testing, use `TestConsole` from `Spectre.Console.Testing`.

## Implementation Tasks

1. [ ] Create `IProjectAnalyzer` interface and implementation
2. [ ] Create `IDockerfileAnalyzer` interface and implementation
3. [ ] Create `IGitSettingsDetector` interface and implementation
4. [ ] Implement path validation and type detection
5. [ ] Build container command generation for .csproj
6. [ ] Implement save to both locations
7. [ ] Add `--id`, `--image-name`, `--image-registry` options
8. [ ] Implement command handler with `IAnsiConsole` injection
9. [ ] Add unit tests (using `TestConsole`)
10. [ ] Add integration tests

## Definition of Done

- [ ] Command accepts path to .csproj, Dockerfile, or directory
- [ ] Directory paths auto-detect .csproj first, then Dockerfile
- [ ] .csproj resources get both modes configured
- [ ] Dockerfile resources get container mode only
- [ ] ID is prompted when not provided via --id
- [ ] Git settings auto-detected from current repo
- [ ] Saves to both repository settings and global config
- [ ] Smart defaults with override flags
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No compiler warnings
- [ ] Code review approved
