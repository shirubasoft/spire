using CliWrap;

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
            if (await IsCommandAvailableAsync(envRuntime, cancellationToken))
            {
                return envRuntime;
            }

            throw new InvalidOperationException(
                $"Container runtime '{envRuntime}' specified by {AspireContainerRuntimeEnvVar} was not found.");
        }

        // 2. Check if docker is available
        if (await IsCommandAvailableAsync(DockerCommand, cancellationToken))
        {
            return DockerCommand;
        }

        // 3. Check if podman is available
        if (await IsCommandAvailableAsync(PodmanCommand, cancellationToken))
        {
            return PodmanCommand;
        }

        throw new InvalidOperationException(
            "No container runtime found. Install docker or podman, or set the "
            + $"{AspireContainerRuntimeEnvVar} environment variable.");
    }

    private static async Task<bool> IsCommandAvailableAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await CliWrap.Cli.Wrap(command)
                .WithArguments("--version")
                .WithStandardOutputPipe(PipeTarget.ToStream(Stream.Null))
                .WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(cancellationToken);
            return result.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Command not found on PATH
            return false;
        }
    }
}