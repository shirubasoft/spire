using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Wraps an <see cref="IResourceBuilder{T}"/> to provide mode-aware configuration.
/// Implements <see cref="IResourceBuilder{T}"/> so that all Aspire extension
/// methods (e.g. <c>WithHttpEndpoint</c>, <c>WaitFor</c>) work directly.
/// </summary>
public class SharedResourceBuilder<T> : IResourceBuilder<T> where T : SharedResource
{
    private readonly IResourceBuilder<IResource> _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedResourceBuilder{T}"/> class.
    /// </summary>
    public SharedResourceBuilder(IResourceBuilder<IResource> inner, T resource)
    {
        _inner = inner;
        Resource = resource;
    }

    /// <inheritdoc />
    public T Resource { get; }

    /// <inheritdoc />
    public IDistributedApplicationBuilder ApplicationBuilder => _inner.ApplicationBuilder;

    /// <inheritdoc />
    public IResourceBuilder<T> WithAnnotation<TAnnotation>(TAnnotation annotation, ResourceAnnotationMutationBehavior behavior)
        where TAnnotation : IResourceAnnotation
    {
        _inner.WithAnnotation(annotation, behavior);
        return this;
    }
}
