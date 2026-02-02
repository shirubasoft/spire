# Spire

Share .NET Aspire services across multiple repositories. A service runs as a **Project** in its home repo and as a **Container** everywhere else — with a single declaration.

## The Problem

In a multi-repo setup, you want `payments-service` to:

- Run as a **Project** in the `payments` repo (for inner-loop development with hot reload, debugging, etc.)
- Run as a **Container** in the `frontend` repo (consuming it as a dependency)

Aspire has no built-in way to do this. You'd need to maintain separate AppHost configurations, manually build container images, and keep them in sync.

## How It Works

Spire uses a `.aspire-shared-resources.json` file as the source of truth for shared resource definitions. This file lives in your git repository and describes how each service can run (as a project or container).

A global configuration file (`~/.spire/resources.json`) tracks all resources across repositories. MSBuild targets discover `.aspire-shared-resources.json` files by walking up from the project directory to the git root, and a Roslyn source generator creates type-safe builders at compile time.

## Core Flows

### Generate (producer)

Scan an existing repository for projects and containers, then produce a `.aspire-shared-resources.json` file describing them as shared resources.

```bash
spire resource generate <path>
```

This discovers `.csproj` files and container definitions, then writes (or updates) the `.aspire-shared-resources.json` file in the repository. The global config at `~/.spire/resources.json` is also updated to reflect the generated resources.

### Import (consumer)

Import shared resources from `.aspire-shared-resources.json` files found in the current git repository into the global config.

```bash
spire resource import
```

This walks the git repository looking for `.aspire-shared-resources.json` files and merges any missing or updated entries into `~/.spire/resources.json`. The MSBuild targets call this automatically before the source generator runs, so consuming AppHosts stay in sync without manual steps.

### At Build Time

1. MSBuild targets find the git root and discover all `.aspire-shared-resources.json` files between the project directory and the root.
2. The CLI is invoked to sync those files into the global config.
3. The source generator reads the merged configuration and emits type-safe `AddSharedResources()` and `builder.AddResourceName()` methods.

## Resource Management

All resource commands operate on **both** the `.aspire-shared-resources.json` file and the global config. The JSON file is the portable, version-controlled source of truth; the global config is the local runtime state. They are kept in sync:

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
| `spire resource generate <path>` | Generate `.aspire-shared-resources.json` from existing projects/containers |
| `spire resource import` | Import resources from `.aspire-shared-resources.json` in the current git repo |
| `spire resource list` | Show all registered resources |
| `spire resource info <id>` | Show detailed info for a resource |
| `spire resource remove <id>` | Remove a resource from JSON and global config |
| `spire resource clear` | Clear resources from JSON and global config |
| `spire modes` | Toggle Project/Container mode for resources |
| `spire override` | Set runtime overrides (mode, registry rewrites, image rewrites) |

## Configuration

| File | Purpose |
|------|---------|
| `.aspire-shared-resources.json` | Per-repository resource definitions (version controlled) |
| `~/.spire/.aspire-shared-resources.json` | Global config aggregating all resources across repos |

## Requirements

- .NET 10.0 or later
- .NET Aspire 13.1 or later
- Docker or Podman (for container mode)

## License

[MIT](LICENSE)
