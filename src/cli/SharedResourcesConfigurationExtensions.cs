using Microsoft.Extensions.Configuration;

namespace Spire.Cli;

/// <summary>
/// Extension methods for building shared resource configuration with layered priority.
/// </summary>
public static class SharedResourcesConfigurationExtensions
{
    private const string ConfigFileName = "aspire-shared-resources.json";

    /// <summary>
    /// Loads shared resource configuration with layered priority and binds it to
    /// <see cref="GlobalSharedResources"/>. All paths are guaranteed absolute
    /// because the import step converts relative paths when writing to global config.
    /// When <paramref name="level"/> is <see cref="Level.Repo"/>, the repository override
    /// takes priority over global config. When <see cref="Level.Global"/>, global config
    /// takes priority over the repository override.
    /// </summary>
    /// <param name="repositorySlug">Slug identifying the Git repository to load overrides for, or null if not in a repository.</param>
    /// <param name="level">The priority level determining which layer wins.</param>
    public static GlobalSharedResources GetSharedResources(
        string? repositorySlug = null,
        Level level = Level.Repo)
    {
        var globalDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "spire");

        var builder = new ConfigurationBuilder();

        if (repositorySlug is null)
        {
            AddGlobalLayer(builder, globalDir);
        }
        else if (level == Level.Repo)
        {
            // Global first (lowest priority), then repo override on top
            AddGlobalLayer(builder, globalDir);
            AddRepoLayer(builder, globalDir, repositorySlug);
        }
        else
        {
            // Repo first (lowest priority), then global on top
            AddRepoLayer(builder, globalDir, repositorySlug);
            AddGlobalLayer(builder, globalDir);
        }

        var configuration = builder.Build();

        return configuration.Get<GlobalSharedResources>()
            ?? GlobalSharedResources.Empty;
    }

    private static void AddGlobalLayer(IConfigurationBuilder builder, string globalDir)
    {
        builder.AddJsonFile(
            Path.Combine(globalDir, ConfigFileName),
            optional: true,
            reloadOnChange: false);
    }

    private static void AddRepoLayer(
        IConfigurationBuilder builder,
        string globalDir,
        string repositorySlug)
    {
        var repoDir = Path.Combine(globalDir, repositorySlug);
        builder.AddJsonFile(
            Path.Combine(repoDir, ConfigFileName),
            optional: true,
            reloadOnChange: false);
    }
}
