namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Reads shared resources configuration from a repository's .aspire/settings.json file.
/// </summary>
public interface IRepositorySharedResourcesReader
{
    /// <summary>
    /// Reads the shared resources configuration from the specified repository path.
    /// </summary>
    /// <param name="repositoryPath">The path to the repository root.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The repository shared resources, or null if the settings file doesn't exist.</returns>
    Task<RepositorySharedResources?> ReadAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a settings file exists in the specified repository path.
    /// </summary>
    /// <param name="repositoryPath">The path to the repository root.</param>
    /// <returns>True if the settings file exists, false otherwise.</returns>
    bool SettingsFileExists(string repositoryPath);

    /// <summary>
    /// Gets the path to the settings file for the specified repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the repository root.</param>
    /// <returns>The full path to the settings file.</returns>
    string GetSettingsFilePath(string repositoryPath);
}
