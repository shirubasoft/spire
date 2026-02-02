namespace Aspire.Hosting;

/// <summary>
/// The execution mode for a shared resource.
/// </summary>
public enum ResourceMode
{
    /// <summary>
    /// Run the resource as a local .NET project.
    /// </summary>
    Project,

    /// <summary>
    /// Run the resource as a container.
    /// </summary>
    Container,
}
