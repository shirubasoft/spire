# Configuration

Spire uses a layered configuration system with three levels: global, repository-scoped overrides, and per-repo settings files.

## Configuration Files

### `.aspire/settings.json` (repository config)

The portable, version-controlled source of truth. Lives in each AppHost's `.aspire/` directory and uses **relative paths**.

```json
{
  "resources": {
    "my-service": {
      "mode": "Container",
      "containerMode": {
        "imageName": "my-service",
        "imageRegistry": "docker.io",
        "imageTag": "latest",
        "buildCommand": "dotnet publish",
        "buildWorkingDirectory": "./src/MyService"
      },
      "projectMode": {
        "projectPath": "./src/MyService/MyService.csproj"
      }
    }
  },
  "externalResources": [
    {
      "url": "https://github.com/org/other-repo.git",
      "branch": "main"
    }
  ]
}
```

**External resources** let you reference services from other repositories. On `spire resource import`, these repos are cloned to `~/.aspire/spire/{repo-slug}/` and their resources are imported recursively.

### `~/.aspire/spire/aspire-shared-resources.json` (global config)

Aggregates all resources across repositories with **absolute paths**. This is the runtime state used by the source generator and CLI commands.

```json
{
  "$schema": "https://raw.githubusercontent.com/shirubasoft/spire/main/schemas/shared-resources-global.schema.json",
  "resources": {
    "my-service": {
      "mode": "Container",
      "containerMode": { ... },
      "projectMode": { ... },
      "gitRepository": {
        "url": "https://github.com/org/repo.git",
        "defaultBranch": "main"
      }
    }
  }
}
```

### `~/.aspire/spire/{repo-slug}/aspire-shared-resources.json` (repository overrides)

Per-repository overrides with absolute paths. Created by CLI commands to customize resource settings for a specific consuming repository.

## Configuration Layering

Configuration is resolved with increasing priority:

1. **Global config** — base resource definitions
2. **Repository overrides** — per-repo customizations
3. **Environment variables** (`ASPIRE_*`) — runtime overrides

## Which Commands Affect Which Files

| Command | `.aspire/settings.json` | Global config |
|---------|------------------------|---------------|
| `resource generate` | Creates/updates | Updates |
| `resource import` | Reads | Updates |
| `resource remove` | Removes entry | Removes entry |
| `resource clear` | Clears (with `--include-repo`) | Clears |
| `resource list` | — | Reads |
| `resource info` | — | Reads |

## MSBuild Properties

Set these in your AppHost `.csproj` or `Directory.Build.props`.

| Property | Default | Description |
|----------|---------|-------------|
| `SkipSharedResourceResolution` | `false` | Skip all Spire MSBuild targets (CLI check, import, source generation). |
| `SpireAutoInstallCli` | `false` | Auto-install or update the `spire` CLI tool at build time. |
| `EnableSdkContainerSupport` | `true` | Enables .NET SDK container support (set by Spire props). |
| `InterceptorsNamespaces` | `Spire.Hosting.Generated` | Namespace for interceptor-generated code (set by Spire props). |

### Automatic CLI Install

Set `SpireAutoInstallCli` to `true` in your AppHost project:

```xml
<PropertyGroup>
  <SpireAutoInstallCli>true</SpireAutoInstallCli>
</PropertyGroup>
```

When enabled, the build runs `dotnet tool install -g spire.cli` before checking CLI availability. If the install fails (e.g., no network), the build continues and falls back to the normal availability check.

## JSON Schemas

Spire provides JSON schemas for configuration validation:

| Schema | Validates |
|--------|-----------|
| [`aspire-settings.schema.json`](../schemas/aspire-settings.schema.json) | `.aspire/settings.json` files |
| [`shared-resources-global.schema.json`](../schemas/shared-resources-global.schema.json) | Global config (`~/.aspire/spire/aspire-shared-resources.json`) |
| [`shared-resources.schema.json`](../schemas/shared-resources.schema.json) | Shared definitions (referenced by the above schemas) |

Use the `$schema` property in your JSON files to enable editor validation and autocompletion.
