namespace Spire.Cli.Services;

/// <summary>
/// Executes shell commands and captures their output.
/// </summary>
public interface ICommandRunner
{
    /// <summary>
    /// Runs a command asynchronously with the specified arguments.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="workingDirectory">The working directory for the command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CommandRunResult> RunAsync(
        string command,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken);

    /// <summary>
    /// Runs a command asynchronously, piping output to the provided handlers.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="workingDirectory">The working directory for the command.</param>
    /// <param name="stdOutHandler">Handler for standard output lines.</param>
    /// <param name="stdErrHandler">Handler for standard error lines.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code of the command.</returns>
    Task<int> RunWithOutputAsync(
        string command,
        string arguments,
        string workingDirectory,
        Action<string> stdOutHandler,
        Action<string> stdErrHandler,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents the result of a command execution.
/// </summary>
public sealed record CommandRunResult
{
    /// <summary>
    /// The exit code of the command.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// The standard output of the command.
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    /// The standard error output of the command.
    /// </summary>
    public required string StandardError { get; init; }

    /// <summary>
    /// Indicates whether the command succeeded (exit code 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;
}
