# Spire

Share .NET Aspire services across multiple repositories. A service runs as a **Project** in its home repo and as a **Container** everywhere else — with a single declaration.

## The Problem

In a multi-repo setup, you want `payments-service` to:

- Run as a **Project** in the `payments` repo (inner-loop development with hot reload, debugging, etc.)
- Run as a **Container** in the `frontend` repo (consuming it as a dependency)

Aspire has no built-in way to do this. You'd need to maintain separate AppHost configurations, manually build container images, and keep them in sync.

## Quick Start

### 1. Install

```bash
# Add the hosting package to your AppHost
dotnet add package Shirubasoft.Spire.Hosting

# Install the CLI
dotnet tool install -g spire.cli
```

> To auto-install the CLI at build time, set `<SpireAutoInstallCli>true</SpireAutoInstallCli>` in your AppHost project. See [Configuration](docs/configuration.md#automatic-cli-install) for details.

### 2. Generate a shared resource (producer repo)

```bash
spire resource generate ./src/MyService
```

This scans the path for a `.csproj` or `Dockerfile`, creates a resource definition in `.aspire/settings.json`, and registers it in the global config.

### 3. Import resources (consumer repo)

```bash
spire resource import
```

This happens automatically at build time via MSBuild targets, but you can run it manually too.

### 4. Use in your AppHost

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddSharedResourcesConfiguration();

var api = builder.AddPaymentsService()        // generated method
    .ConfigureContainer(b => b.WithHttpEndpoint(targetPort: 8080))
    .WithHttpHealthCheck("/health");

builder.Build().Run();
```

The source generator creates type-safe `Add{Name}()` extension methods for each registered resource. See [Aspire Extension Methods](docs/aspire-extension-methods.md) for how standard Aspire methods like `.WithHttpEndpoint()` and `.WaitFor()` work on shared resources.

### 5. Toggle modes

```bash
# Interactive
spire modes

# Non-interactive
spire modes --id payments-service --mode Container
```

## CLI Commands

| Command | Description |
|---------|-------------|
| `spire build` | Build container images for shared resources |
| `spire resource generate <path>` | Generate resource definitions from a project or Dockerfile |
| `spire resource import` | Import resources from `.aspire/settings.json` into global config |
| `spire resource list` | List all registered resources (JSON output) |
| `spire resource info --id <id>` | Show detailed info for a resource |
| `spire resource remove --id <id>` | Remove a resource |
| `spire resource clear` | Clear all or specific resources |
| `spire modes` | Toggle Project/Container mode |
| `spire override` | Override resource configurations per-repo *(placeholder)* |

See [CLI Reference](docs/cli-reference.md) for all flags and options.

## Configuration Files

| File | Purpose |
|------|---------|
| `.aspire/settings.json` | Per-repo resource definitions (relative paths, version-controlled) |
| `~/.aspire/spire/aspire-shared-resources.json` | Global config (absolute paths, local state) |
| `~/.aspire/spire/{repo-slug}/aspire-shared-resources.json` | Repository-scoped overrides |

See [Configuration](docs/configuration.md) for details on layering, MSBuild properties, and JSON schemas.

## How It Works

At build time, MSBuild targets invoke the Spire CLI to import resources and forward them to a Roslyn source generator that emits type-safe builders. At runtime, resources resolve to either a `ProjectResource` or `ContainerResource` based on their configured mode.

See [Architecture](docs/architecture.md) for the full build-time and runtime flow.

## Documentation

- [CLI Reference](docs/cli-reference.md) — full command, flag, and option reference
- [Configuration](docs/configuration.md) — config files, layering, MSBuild properties, JSON schemas
- [Architecture](docs/architecture.md) — build-time flow, source generator, runtime behavior
- [Aspire Extension Methods](docs/aspire-extension-methods.md) — using standard Aspire methods on shared resources

## Requirements

- .NET 10.0 or later
- .NET Aspire 13.1 or later
- Docker or Podman (for container mode)

## License

[MIT](LICENSE)
