using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Wraps an <see cref="IResourceBuilder{T}"/> to provide mode-aware configuration.
/// </summary>
public abstract class ResourceBuilderProxy
{
    private readonly IResourceBuilder<IResource> _inner;
    private readonly ResourceMode _mode;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceBuilderProxy"/> class.
    /// </summary>
    protected ResourceBuilderProxy(IResourceBuilder<IResource> inner, ResourceMode mode)
    {
        _inner = inner;
        _mode = mode;
    }

    /// <summary>
    /// Gets the underlying resource builder.
    /// </summary>
    public IResourceBuilder<IResource> Inner => _inner;

    /// <summary>
    /// Configures the resource only when running in container mode.
    /// </summary>
    public ResourceBuilderProxy ConfigureContainer(Action<IResourceBuilder<IResource>> configure)
    {
        if (_mode == ResourceMode.Container)
        {
            configure(_inner);
        }

        return this;
    }

    /// <summary>
    /// Configures the resource only when running in project mode.
    /// </summary>
    public ResourceBuilderProxy ConfigureProject(Action<IResourceBuilder<IResource>> configure)
    {
        if (_mode == ResourceMode.Project)
        {
            configure(_inner);
        }

        return this;
    }

    /// <summary>
    /// Configures the resource regardless of mode.
    /// </summary>
    public ResourceBuilderProxy Configure<T>(Action<IResourceBuilder<T>> configure) where T : IResource
    {
        configure((IResourceBuilder<T>)_inner);
        return this;
    }
}
