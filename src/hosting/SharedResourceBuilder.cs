using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Wraps an <see cref="IResourceBuilder{T}"/> to provide mode-aware configuration.
/// </summary>
public abstract class SharedResourceBuilder
{
    private readonly IResourceBuilder<IResource> _inner;
    private readonly ResourceMode _mode;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedResourceBuilder"/> class.
    /// </summary>
    protected SharedResourceBuilder(IResourceBuilder<IResource> inner, ResourceMode mode)
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
    public SharedResourceBuilder ConfigureContainer(Action<IResourceBuilder<ContainerResource>> configure)
    {
        if (_mode == ResourceMode.Container)
        {
            configure((IResourceBuilder<ContainerResource>)_inner);
        }

        return this;
    }

    /// <summary>
    /// Configures the resource only when running in project mode.
    /// </summary>
    public SharedResourceBuilder ConfigureProject(Action<IResourceBuilder<ProjectResource>> configure)
    {
        if (_mode == ResourceMode.Project)
        {
            configure((IResourceBuilder<ProjectResource>)_inner);
        }

        return this;
    }

    /// <summary>
    /// Configures the resource regardless of mode.
    /// </summary>
    public SharedResourceBuilder Configure<T>(Action<IResourceBuilder<T>> configure) where T : IResource
    {
        configure((IResourceBuilder<T>)_inner);
        return this;
    }
}