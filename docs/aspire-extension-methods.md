# Using Aspire Extension Methods on Shared Resources

Spire shared resources support all standard Aspire extension methods out of the box. Methods like `.WithHttpEndpoint()`, `.WithHttpHealthCheck()`, `.WithReference()`, and `.WaitFor()` work directly on shared resource builders, just as they would on a regular Aspire project or container resource.

## How it works

When you call an Aspire extension method on a shared resource, Spire automatically intercepts the call and forwards it to the underlying resource (either a `ContainerResource` or `ProjectResource`, depending on the current mode). This happens transparently at compile time through a source generator -- no runtime reflection or manual wiring required.

```csharp
var apiService = builder.AddSampleApiservice()
    .WithHttpHealthCheck("/health");

builder.AddSampleWeb()
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService.GetEndpoint("http"))
    .WaitFor(apiService);
```

Every chained call above is intercepted and forwarded to the inner builder with the same arguments you provided. If the resource is in container mode, the call goes to the `ContainerResource` builder; if in project mode, it goes to the `ProjectResource` builder.

## Mode-specific configuration

Some configuration only makes sense for one mode. Use `ConfigureContainer` and `ConfigureProject` to scope configuration to a specific mode:

```csharp
builder.AddSampleApiservice()
    .ConfigureContainer(b => b.WithHttpEndpoint(targetPort: 8080))
    .ConfigureProject(b => b.WithHttpsEndpoint(targetPort: 5001))
    .WithHttpHealthCheck("/health");
```

- `ConfigureContainer` -- only runs when the resource is in container mode
- `ConfigureProject` -- only runs when the resource is in project mode

These are regular extension methods (not intercepted) and receive the typed inner builder directly.

## What gets intercepted

Any Aspire extension method called on a shared resource builder is intercepted **except**:

- `ConfigureContainer` and `ConfigureProject` (these are Spire's own extensions and handle mode routing themselves)

The interceptor forwards only the arguments you explicitly provide. If you call `.WithHttpHealthCheck("/health")`, the forwarded call is `inner.WithHttpHealthCheck(path: path)` -- default parameters you didn't specify are left to the inner method's own defaults.

## Fluent chaining

Intercepted methods return the original shared resource builder, so fluent chaining works naturally:

```csharp
builder.AddPaymentsService()
    .ConfigureContainer(b => b.WithHttpEndpoint(targetPort: 8080))
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WaitFor(database);
```

Each call in the chain is intercepted independently. You can mix Spire-specific methods (`ConfigureContainer`, `ConfigureProject`) with standard Aspire methods in any order.

## Limitations

- **Same-mode forwarding**: The interceptor forwards the call to both container and project paths based on the runtime mode. If an extension method only exists for one resource type (e.g., a container-only method), use `ConfigureContainer` or `ConfigureProject` instead.
- **Generic constraints**: The interceptor constrains `T` to `SharedResource`. Methods with more specific type constraints (e.g., `where T : IResourceWithConnectionString`) work as long as `SharedResource` implements the required interface.
