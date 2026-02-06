# CLI Reference

Complete reference for all Spire CLI commands, arguments, and options.

## spire build

Build container images for shared resources.

```bash
spire build [options]
```

| Option | Short | Type | Default | Description |
|--------|-------|------|---------|-------------|
| `--ids` | | `string[]` | | Resource IDs to build. Builds all if omitted. |
| `--global` | `-g` | `bool` | `false` | Build all resources from global config. |
| `--force` | `-f` | `bool` | `false` | Rebuild even if the commit-tagged image already exists. |

**Resolution order** when no `--ids` are provided:

1. If `--global` is set, builds all resources with container mode settings from global config.
2. If inside a git repo, loads resources from `.aspire/settings.json` and filters to those with container mode in global config.
3. Otherwise, prompts the user.

**Image tags**: Each build produces three tags — `commit:<hash>`, `branch:<sanitized-name>`, and `latest`. If the commit tag already exists, the build is skipped unless `--force` is used.

## spire modes

Toggle Project/Container mode for shared resources.

```bash
spire modes [options]
```

| Option | Short | Type | Default | Description |
|--------|-------|------|---------|-------------|
| `--id` | `-i` | `string` | | Resource ID (for non-interactive use). |
| `--mode` | `-m` | `Mode` | | Target mode: `Container` or `Project`. |

**Interactive mode** (no options): displays a menu of all resources with their current modes and lets you toggle one.

**Non-interactive mode**: both `--id` and `--mode` must be provided together.

## spire resource generate

Generate a shared resource definition from an existing project or container.

```bash
spire resource generate <path> [options]
```

| Argument | Type | Required | Description |
|----------|------|----------|-------------|
| `path` | `string` | Yes | Path to a `.csproj` file, `Dockerfile`, or directory containing either. |

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--id` | `string` | | Resource identifier. Prompted interactively if not provided. |
| `--image-name` | `string` | | Override the container image name (defaults to project name). |
| `--image-registry` | `string` | | Override the image registry (defaults to `docker.io`). |
| `--yes` | `bool` | `false` | Skip confirmation prompts. |

**What it does**:

1. Analyzes the path for a `.csproj` or `Dockerfile`.
2. Detects git repository settings (URL, default branch).
3. Creates both global (`~/.aspire/spire/aspire-shared-resources.json`) and repository (`.aspire/settings.json`) configurations.

## spire resource import

Import shared resources from `.aspire/settings.json` files in the current git repository into the global config.

```bash
spire resource import [options]
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--yes` | `bool` | `false` | Skip confirmation prompts. |
| `--force` | `bool` | `false` | Overwrite existing resources in global config. |

Must be run from within a git repository. Also resolves **external resources** — repositories referenced in the `externalResources` section of `.aspire/settings.json` are cloned to `~/.aspire/spire/{repo-slug}/` and their resources are imported recursively.

## spire resource list

List all registered shared resources.

```bash
spire resource list
```

No options. Outputs JSON to stdout, suitable for piping to other tools or consumption by the source generator.

## spire resource info

Show detailed information about a single shared resource.

```bash
spire resource info --id <id>
```

| Option | Short | Type | Required | Description |
|--------|-------|------|----------|-------------|
| `--id` | `-i` | `string` | Yes | The resource identifier. |

## spire resource remove

Remove a shared resource from both global config and repository settings.

```bash
spire resource remove --id <id> [options]
```

| Option | Short | Type | Default | Description |
|--------|-------|------|---------|-------------|
| `--id` | `-i` | `string` | *(required)* | The resource identifier. |
| `--yes` | `-y` | `bool` | `false` | Skip confirmation prompt. |

## spire resource clear

Clear all shared resources, or specific ones.

```bash
spire resource clear [options]
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--ids` | `string[]` | | Resource IDs to clear. Clears all if omitted. |
| `--include-repo` | `bool` | `false` | Also clear from repository settings (`.aspire/settings.json`). |
| `--yes` | `bool` | `false` | Skip confirmation prompt. |

## spire override

Override resource configurations for the current git repository or globally.

```bash
spire override
```

> This command is currently a placeholder with no subcommands implemented.

## Exit Codes

All commands return `0` on success and `1` on error.
