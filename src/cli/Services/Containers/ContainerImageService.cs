using Spectre.Console;

namespace Spire.Cli.Services;

/// <summary>
/// Provides operations for managing container images using docker or podman.
/// </summary>
public sealed class ContainerImageService : IContainerImageService
{
    private readonly IContainerRuntimeResolver _runtimeResolver;
    private readonly ICommandRunner _commandRunner;
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerImageService"/> class.
    /// </summary>
    /// <param name="runtimeResolver">The container runtime resolver.</param>
    /// <param name="commandRunner">The command runner.</param>
    /// <param name="console">The console for output.</param>
    public ContainerImageService(
        IContainerRuntimeResolver runtimeResolver,
        ICommandRunner commandRunner,
        IAnsiConsole console)
    {
        _runtimeResolver = runtimeResolver;
        _commandRunner = commandRunner;
        _console = console;
    }

    /// <inheritdoc />
    public async Task<bool> ImageExistsAsync(ContainerImage image, CancellationToken cancellationToken)
    {
        var runtime = await _runtimeResolver.ResolveAsync(cancellationToken);
        var fullImageRef = $"{image.ImageRegistry}/{image.ImageName}:{image.ImageTag}";

        var result = await _commandRunner.RunAsync(
            runtime,
            $"image inspect {fullImageRef}",
            Directory.GetCurrentDirectory(),
            cancellationToken);

        return result.IsSuccess;
    }

    /// <inheritdoc />
    public async Task BuildImageAsync(ContainerImageBuildRequest request, CancellationToken cancellationToken)
    {
        // Parse the build command to get the executable and arguments
        var parts = request.Command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new InvalidOperationException("Build command is empty.");
        }

        var executable = parts[0];
        var arguments = parts.Length > 1 ? parts[1] : string.Empty;

        _console.MarkupLine($"[grey]Running: {request.Command}[/]");

        var exitCode = await _commandRunner.RunWithOutputAsync(
            executable,
            arguments,
            request.WorkingDirectory,
            line => _console.WriteLine(line),
            line => _console.MarkupLine($"[red]{line.EscapeMarkup()}[/]"),
            cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Build command failed with exit code {exitCode}.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> TagExistsAsync(string registry, string imageName, string tag, CancellationToken cancellationToken)
    {
        var runtime = await _runtimeResolver.ResolveAsync(cancellationToken);
        var fullImageRef = string.IsNullOrEmpty(registry)
            ? $"{imageName}:{tag}"
            : $"{registry}/{imageName}:{tag}";

        var result = await _commandRunner.RunAsync(
            runtime,
            $"image inspect {fullImageRef}",
            Directory.GetCurrentDirectory(),
            cancellationToken);

        return result.IsSuccess;
    }

    /// <inheritdoc />
    public async Task TagImageAsync(string sourceImage, string targetBaseImage, IEnumerable<string> tags, CancellationToken cancellationToken)
    {
        var runtime = await _runtimeResolver.ResolveAsync(cancellationToken);

        foreach (var tag in tags)
        {
            var targetImage = $"{targetBaseImage}:{tag}";
            var result = await _commandRunner.RunAsync(
                runtime,
                $"tag {sourceImage} {targetImage}",
                Directory.GetCurrentDirectory(),
                cancellationToken);

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to tag image as {targetImage}: {result.StandardError}");
            }

            _console.MarkupLine($"[green]Tagged: {targetImage}[/]");
        }
    }
}