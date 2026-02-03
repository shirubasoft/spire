namespace Spire.Cli.Services;

/// <summary>
/// Resolves the container runtime to use for building and managing images.
/// </summary>
public interface IContainerRuntimeResolver
{
    /// <summary>
    /// Resolves the container runtime command to use.
    /// Selection order: $ASPIRE_CONTAINER_RUNTIME environment variable, docker (if available), podman (fallback).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The container runtime command (e.g., "docker" or "podman").</returns>
    Task<string> ResolveAsync(CancellationToken cancellationToken);
}