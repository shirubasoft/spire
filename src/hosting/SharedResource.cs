#pragma warning disable ASPIREPROBES001

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Decorator that wraps an inner <see cref="IResource"/> and implements all common
/// marker interfaces so that <see cref="IResourceBuilder{T}"/> extension methods
/// (e.g. <c>WithHttpEndpoint</c>, <c>WaitFor</c>) work directly.
/// </summary>
public class SharedResource
    : IResourceWithEnvironment, IResourceWithArgs,
      IResourceWithEndpoints, IResourceWithWaitSupport,
      IResourceWithProbes, IComputeResource
{
    private readonly IResource _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedResource"/> class.
    /// </summary>
    public SharedResource(IResource inner, ResourceMode mode, IResourceBuilder<IResource> innerBuilder)
    {
        _inner = inner;
        Mode = mode;
        InnerBuilder = innerBuilder;
    }

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public ResourceAnnotationCollection Annotations => _inner.Annotations;

    /// <summary>
    /// Gets the execution mode for this shared resource.
    /// </summary>
    public ResourceMode Mode { get; }

    /// <summary>
    /// Gets the underlying resource builder.
    /// </summary>
    public IResourceBuilder<IResource> InnerBuilder { get; }
}
