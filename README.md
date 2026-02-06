# Spire

Share .NET Aspire services across multiple repositories. A service runs as a **Project** in its home repo and as a **Container** everywhere else — with a single declaration.

## The Problem

In a multi-repo setup, you want `payments-service` to:

- Run as a **Project** in the `payments` repo (for inner-loop development with hot reload, debugging, etc.)
- Run as a **Container** in the `frontend` repo (consuming it as a dependency)

Aspire has no built-in way to do this. You'd need to maintain separate AppHost configurations, manually build container images, and keep them in sync.

## How It Works

Spire extends Aspire's native `.aspire/settings.json` file with a `sharedResources` section that describes how each service can run (as a project or container). This file lives in the AppHost's `.aspire` directory and uses relative paths.

A global configuration file (`~/.aspire/spire/aspire-shared-resources.json`) aggregates all resources across repositories with absolute paths. Repository-scoped overrides (created via CLI commands) can further customize resources per repo. MSBuild targets invoke the Spire CLI to import resources at build time, and a Roslyn source generator creates type-safe builders at compile time.

## Core Flows

### Generate (producer)

Scan an existing repository for projects and containers, then produce shared resource definitions in `.aspire/settings.json`.

```bash
spire resource generate <path>
```

This discovers `.csproj` files and container definitions, then writes (or updates) the `sharedResources` section in `.aspire/settings.json`. The global config at `~/.aspire/spire/aspire-shared-resources.json` is also updated with absolute paths.

### Import (consumer)

Import shared resources from `.aspire/settings.json` files found in the current git repository into the global config.

```bash
spire resource import
```

This walks the git repository looking for `.aspire/settings.json` files, converts relative paths to absolute, and merges entries into `~/.aspire/spire/aspire-shared-resources.json`. The MSBuild targets call this automatically before the source generator runs, so consuming AppHosts stay in sync without manual steps.

### At Build Time

1. MSBuild targets invoke `spire resource import` to sync `.aspire/settings.json` files into the global config.
2. `spire resource list --level repo --json` outputs the merged configuration as JSON.
3. The source generator reads this JSON, matching the schema structure (`resources` map with `mode`, `containerMode`, `projectMode`), and emits type-safe `builder.Add{ResourceName}()` extension methods.
4. A generated `AddSharedResourcesConfiguration()` method embeds the resolved JSON as an in-memory configuration source, so generated code reads values like `resources:{id}:mode` from `IConfiguration` at runtime.

## Resource Management

All resource commands operate on **both** `.aspire/settings.json` and the global config. The settings file is the portable, version-controlled source of truth; the global config is the local runtime state. Configuration is layered with increasing priority: global → repository overrides → environment variables (`ASPIRE_*`).

| Command | JSON file | Global config |
|---------|-----------|---------------|
| `resource generate` | Creates/updates | Updates |
| `resource import` | Reads | Updates |
| `resource remove` | Removes entry | Removes entry |
| `resource clear` | Clears entries | Clears entries |
| `resource list` | — | Reads |
| `resource info` | — | Reads |

## CLI Commands

| Command | Description |
|---------|-------------|
| `spire build` | Build container images for shared resources |
| `spire resource generate <path>` | Generate `.aspire/settings.json` from existing projects/containers |
| `spire resource import` | Import resources from `.aspire/settings.json` in the current git repo |
| `spire resource list` | Show all registered resources |
| `spire resource info --id <id>` | Show detailed info for a resource |
| `spire resource remove --id <id>` | Remove a resource from JSON and global config |
| `spire resource clear` | Clear resources from JSON and global config |
| `spire modes` | Toggle Project/Container mode for resources |

## Configuration

| File | Purpose |
|------|---------|
| `.aspire/settings.json` | Per-repository resource definitions — relative paths (version controlled) |
| `~/.aspire/spire/aspire-shared-resources.json` | Global config aggregating all resources — absolute paths |
| `~/.aspire/spire/{repo-slug}/aspire-shared-resources.json` | Repository-scoped overrides — absolute paths (CLI only) |

### MSBuild Properties

| Property | Default | Description |
|----------|---------|-------------|
| `SkipSharedResourceResolution` | `false` | Skip all Spire MSBuild targets (CLI checks, import, source generation) |
| `SpireAutoInstallCli` | `false` | Automatically install/update the `spire` CLI tool at build time |

## Installation

Install the Spire CLI as a global .NET tool:

```bash
dotnet tool install -g spire.cli
```

### Automatic CLI Install

If you'd prefer the CLI to be installed (or updated) automatically at build time, set the `SpireAutoInstallCli` property in your AppHost project:

```xml
<PropertyGroup>
  <SpireAutoInstallCli>true</SpireAutoInstallCli>
</PropertyGroup>
```

When enabled, the build will run `dotnet tool install -g spire.cli` before checking CLI availability. If the install fails (e.g., no network), the build continues and falls back to the normal availability check.

## Requirements

- .NET 10.0 or later
- .NET Aspire 13.1 or later
- Docker or Podman (for container mode)

## License

[MIT](LICENSE)
