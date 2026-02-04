using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for configuring <see cref="SharedResource"/> instances
/// based on their execution mode.
/// </summary>
public static class SharedResourceExtensions
{
    /// <summary>
    /// Configures the resource only when running in container mode.
    /// </summary>
    public static IResourceBuilder<T> ConfigureContainer<T>(
        this IResourceBuilder<T> builder,
        Action<IResourceBuilder<ContainerResource>> configure) where T : SharedResource
    {
        if (builder.Resource.Mode == ResourceMode.Container)
        {
            configure((IResourceBuilder<ContainerResource>)builder.Resource.InnerBuilder);
        }

        return builder;
    }

    /// <summary>
    /// Configures the resource only when running in project mode.
    /// </summary>
    public static IResourceBuilder<T> ConfigureProject<T>(
        this IResourceBuilder<T> builder,
        Action<IResourceBuilder<ProjectResource>> configure) where T : SharedResource
    {
        if (builder.Resource.Mode == ResourceMode.Project)
        {
            configure((IResourceBuilder<ProjectResource>)builder.Resource.InnerBuilder);
        }

        return builder;
    }
}
