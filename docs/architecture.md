# Architecture

How Spire connects the CLI, MSBuild targets, source generator, and Aspire runtime.

## Build-Time Flow

When you build an AppHost that references `Shirubasoft.Spire.Hosting`, four MSBuild targets run before compilation:

```
_EnsureSpireCliInstalled  (if SpireAutoInstallCli=true)
        ↓
_CheckSpireCliAvailability
        ↓
_ImportSharedResources    (spire resource import --yes)
        ↓
_ForwardSharedResourcesToGenerator  (spire resource list → SharedResources.g.json)
        ↓
    CoreCompile           (source generator reads SharedResources.g.json)
```

1. **`_EnsureSpireCliInstalled`** — Runs `dotnet tool install -g spire.cli` if `SpireAutoInstallCli` is `true`. Warns but doesn't fail if installation fails.
2. **`_CheckSpireCliAvailability`** — Verifies `spire --version` succeeds. Fails the build if the tool isn't found.
3. **`_ImportSharedResources`** — Runs `spire resource import --yes` to sync `.aspire/settings.json` into the global config. Also resolves external repository references.
4. **`_ForwardSharedResourcesToGenerator`** — Runs `spire resource list`, writes the JSON output to `$(IntermediateOutputPath)/SharedResources.g.json`, and registers it as an `AdditionalFile` for the source generator.

All targets are skipped if `SkipSharedResourceResolution` is `true`.

## Source Generator

The `SharedResourceGenerator` is a Roslyn `IIncrementalGenerator` that reads `SharedResources.g.json` and emits:

### Per resource

- **`{Name}Resource`** — A class extending `SharedResource` that represents the resource type.
- **`Add{Name}()`** — An extension method on `IDistributedApplicationBuilder` that registers the resource. Reads configuration to determine mode, resolves the inner builder (container or project), and returns `IResourceBuilder<{Name}Resource>`.
- **`{Name}ProjectMetadata`** — If a project path is available at build time, an `IProjectMetadata` implementation for Aspire's project discovery.

### Global

- **`AddSharedResourcesConfiguration()`** — Extension method on `IConfigurationBuilder` that injects the resolved JSON as an in-memory configuration source, making `resources:{id}:mode` available to `IConfiguration`.

### Interceptor

The source generator also emits **interceptors** for Aspire extension method calls on shared resource builders. When you write:

```csharp
builder.AddMyService().WithHttpEndpoint(targetPort: 8080);
```

The interceptor redirects `.WithHttpEndpoint()` to call it on the inner builder (container or project), then returns the shared resource builder to preserve fluent chaining. This is powered by the experimental `GetInterceptableLocation` API in Roslyn.

## Runtime Types

### `SharedResource`

A decorator that wraps an inner `IResource` (either `ContainerResource` or `ProjectResource`) and implements all common Aspire interfaces:

- `IResourceWithEnvironment`
- `IResourceWithArgs`
- `IResourceWithEndpoints`
- `IResourceWithWaitSupport`
- `IResourceWithProbes`
- `IComputeResource`

### `SharedResourceBuilder<T>`

Wraps an `IResourceBuilder<IResource>` to provide mode-aware configuration while preserving the generic type `T` through Aspire's fluent API chains.

### `SharedResourceExtensions`

Provides `ConfigureContainer` and `ConfigureProject` extension methods for mode-scoped configuration:

```csharp
builder.AddMyService()
    .ConfigureContainer(b => b.WithHttpEndpoint(targetPort: 8080))
    .ConfigureProject(b => b.WithHttpsEndpoint(targetPort: 5001));
```

These are **not intercepted** — they receive the typed inner builder directly and only execute when the resource is in the matching mode.

## Solution Structure

```
src/
  core/               Domain models (SharedResource, configs, settings)
  cli/                CLI tool (System.CommandLine, "spire" command)
  hosting/            Runtime types for Aspire AppHost + MSBuild targets
  source-generator/   Roslyn IIncrementalGenerator (netstandard2.0)
tests/                Mirrors src/ structure
schemas/              JSON schemas for config validation
sample/               Example multi-project Aspire setup
```

## Key Interfaces

| Interface | Purpose |
|-----------|---------|
| `IGitService` | Git operations (repo root, branch, commit) |
| `IContainerImageService` | Container build, tag, push, existence checks |
| `IImageTagGenerator` | Generates commit, branch, and latest tags |
| `IRepositorySharedResourcesReader` | Reads `.aspire/settings.json` |
| `IGlobalSharedResourcesReader` | Reads global config |
| `ISharedResourcesWriter` | Writes both global and repo configs |
| `ICommandRunner` | CLI process execution (wraps CliWrap) |
| `IContainerRuntimeResolver` | Detects docker/podman availability |
| `IProjectAnalyzer` | Analyzes `.csproj` files |
| `IDockerfileAnalyzer` | Analyzes Dockerfiles |
| `IGitSettingsDetector` | Detects git repo URL and default branch |
