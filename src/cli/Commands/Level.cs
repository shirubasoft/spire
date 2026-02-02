namespace Spire.Cli;

/// <summary>
/// The scope level for a command.
/// </summary>
public enum Level
{
    /// <summary>
    /// Scoped to the current Git repository.
    /// </summary>
    Repo,

    /// <summary>
    /// Applied globally across all repositories.
    /// </summary>
    Global
}
