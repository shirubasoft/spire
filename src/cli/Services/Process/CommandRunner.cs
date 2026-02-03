using CliWrap;
using CliWrap.Buffered;

namespace Spire.Cli.Services;

/// <summary>
/// Executes shell commands using CliWrap.
/// </summary>
public sealed class CommandRunner : ICommandRunner
{
    /// <inheritdoc />
    public async Task<CommandRunResult> RunAsync(
        string command,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var result = await CliWrap.Cli.Wrap(command)
            .WithArguments(arguments)
            .WithWorkingDirectory(workingDirectory)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken);

        return new CommandRunResult
        {
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError
        };
    }

    /// <inheritdoc />
    public async Task<int> RunWithOutputAsync(
        string command,
        string arguments,
        string workingDirectory,
        Action<string> stdOutHandler,
        Action<string> stdErrHandler,
        CancellationToken cancellationToken)
    {
        var result = await CliWrap.Cli.Wrap(command)
            .WithArguments(arguments)
            .WithWorkingDirectory(workingDirectory)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(stdOutHandler))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(stdErrHandler))
            .ExecuteAsync(cancellationToken);

        return result.ExitCode;
    }
}