# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is Spire?

Spire is a .NET tool for multi-repository service sharing in .NET Aspire. It lets services run as **Projects** in their home repo (inner-loop dev) and as **Containers** in consuming repos, from a single configuration. The CLI manages shared resource registration, mode toggling, and container image builds.

## Build & Test Commands

```bash
dotnet build                                        # Build all projects
dotnet test                                         # Run all tests
dotnet test --project tests/cli                     # Run CLI tests only
dotnet test --project tests/core                    # Run core tests only
dotnet test -- --treenode-filter "/*/*/MySpecs/**"   # Run a single test class (TUnit filter)
dotnet pack -c Release -o artifacts                 # Create NuGet packages
```

Tests use **TUnit** with the **Microsoft.Testing.Platform** runner (configured in `global.json`). CLI tests use **Spectre.Console.Testing** (`TestConsole`) and **NSubstitute** for mocking.

## Solution Structure

```
src/core/           Spire.csproj             Domain models (SharedResource, GlobalSharedResources, RepositorySharedResources)
src/cli/            Spire.Cli.csproj         CLI tool (dotnet tool, command: "spire"), uses System.CommandLine
src/hosting/        Spire.Hosting.csproj     Runtime types for Aspire AppHost (SharedResourceBuilder, ResourceMode)
src/source-generator/ Spire.SourceGenerator.csproj  Roslyn IIncrementalGenerator (netstandard2.0, generates typed Add* methods)
tests/              Mirrors src/ structure
```

## Architecture

### CLI Command Structure

Entry point: `Program.cs` -> `SpireCli.RunAsync()` (in `Commands/CommandApp.cs`) builds the `System.CommandLine.RootCommand` tree:

```
spire
├── build       Build container images (auto-resolves resources from --ids, --global, or current repo)
├── resource    Manage shared resources (generate, import, list, info, remove, clear)
├── modes       Toggle Project/Container mode
└── override    Set runtime overrides
```

Each command has a `*Command.cs` (defines args/options, wires handler) and a `*Handler.cs` (business logic). Handlers receive dependencies via constructor and expose `ExecuteAsync()`.

### Configuration Layering

1. **Global config**: `~/.aspire/spire/aspire-shared-resources.json` (absolute paths)
2. **Repository overrides**: `~/.aspire/spire/{repo-slug}/aspire-shared-resources.json`
3. **Version-controlled**: `.aspire/settings.json` (relative paths, portable)

`GlobalSharedResources` uses absolute paths; `RepositorySharedResources` uses relative paths. Both are immutable records with `Update`/`Remove`/`Clear` methods that return new instances.

### MSBuild Integration (src/hosting/build/)

`Spire.Hosting.targets` runs three build targets before compilation:

1. `_CheckSpireCliAvailability` - verifies `spire` is on PATH
2. `_ImportSharedResources` - runs `spire resource import`
3. `_ForwardSharedResourcesToGenerator` - runs `spire resource list` and writes `SharedResources.g.json` to `obj/`

### Source Generator

`SharedResourceGenerator` (IIncrementalGenerator) reads `SharedResources.g.json` and generates per-resource:

- `I{Name}ResourceBuilder` interface
- `{Name}ResourceBuilder` class (extends `SharedResourceBuilder`)
- `Add{Name}()` extension method on `IDistributedApplicationBuilder`
- `{Name}ProjectMetadata` (if project path is known at build time)

The generator targets **netstandard2.0** and has global analyzers excluded via `Directory.Build.props` (`IsRoslynComponent` condition).

### Key Service Interfaces

- `IGitService` / `IGitCliResolver` - git operations (repo root, branch, commit)
- `IContainerImageService` / `IContainerRuntimeResolver` - container build/tag/push (docker/podman)
- `IImageTagGenerator` - generates commit, branch, and latest tags
- `IRepositorySharedResourcesReader` / `ISharedResourcesWriter` - config file I/O
- `ICommandRunner` - CLI process execution (wraps CliWrap)

## Testing Conventions

- One test class per scenario, named after the scenario (e.g., `ValidResourceBuildSpecs`, `ResourceNotFoundSpecs`)
- Test methods describe behavior: `Execute_WithValidIds_BuildsImages()`
- Tests mirror the source folder structure: `tests/cli/Commands/Build/` corresponds to `src/cli/Commands/Build/`

## Versioning

Uses **Nerdbank.GitVersioning** (`version.json`, currently `0.1.0`). Semantic version is derived from git history. Release branches: `main` and `v*` tags.
