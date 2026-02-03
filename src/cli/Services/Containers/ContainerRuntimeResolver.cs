using CliWrap;
using CliWrap.Exceptions;

namespace Spire.Cli.Services;

/// <summary>
/// Resolves the container runtime by checking environment variable, then docker, then podman.
/// </summary>
public sealed class ContainerRuntimeResolver : IContainerRuntimeResolver
{
    private const string AspireContainerRuntimeEnvVar = "ASPIRE_CONTAINER_RUNTIME";
    private const string DockerCommand = "docker";
    private const string PodmanCommand = "podman";

    /// <inheritdoc />
    public async Task<string> ResolveAsync(CancellationToken cancellationToken)
    {
        // 1. Check environment variable first
        var envRuntime = Environment.GetEnvironmentVariable(AspireContainerRuntimeEnvVar);
        if (!string.IsNullOrWhiteSpace(envRuntime))
        {
            return envRuntime;
        }

        // 2. Check if docker is available
        if (await IsCommandAvailableAsync(DockerCommand, cancellationToken))
        {
            return DockerCommand;
        }

        // 3. Fallback to podman
        return PodmanCommand;
    }

    private static async Task<bool> IsCommandAvailableAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            await CliWrap.Cli.Wrap(command)
                .WithArguments("--version")
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync(cancellationToken);
            return true;
        }
        catch (CommandExecutionException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Command not found on PATH
            return false;
        }
    }
}
