namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Reads repository shared resources configuration from disk.
/// </summary>
public interface IRepositorySharedResourcesReader
{
    /// <summary>
    /// Reads the repository shared resources from .aspire/settings.json at the given repository path.
    /// </summary>
    /// <param name="repoPath">The root path of the repository.</param>
    /// <returns>The repository shared resources, or null if the file does not exist.</returns>
    RepositorySharedResources? Read(string repoPath);

    /// <summary>
    /// Checks if repository shared resources exist at the given path.
    /// </summary>
    /// <param name="repoPath">The root path of the repository.</param>
    /// <returns>True if the settings file exists; otherwise, false.</returns>
    bool Exists(string repoPath);

    /// <summary>
    /// Gets the full path to the settings file for a repository.
    /// </summary>
    /// <param name="repoPath">The root path of the repository.</param>
    /// <returns>The full path to the .aspire/settings.json file.</returns>
    string GetSettingsPath(string repoPath);
}
