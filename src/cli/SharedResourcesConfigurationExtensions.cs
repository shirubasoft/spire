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
    /// When <paramref name="level"/> is <see cref="Level.Repo"/>, repository overrides
    /// take priority over global config. When <see cref="Level.Global"/>, global config
    /// takes priority over repository overrides.
    /// </summary>
    /// <param name="repositorySlugs">Slugs identifying the Git repositories to load overrides for.</param>
    /// <param name="level">The priority level determining which layer wins.</param>
    public static GlobalSharedResources GetSharedResources(
        IEnumerable<string> repositorySlugs,
        Level level = Level.Repo)
    {
        var globalDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "spire");

        var builder = new ConfigurationBuilder();

        if (level == Level.Repo)
        {
            // Global first (lowest priority), then repo overrides on top
            AddGlobalLayer(builder, globalDir);
            AddRepoLayers(builder, globalDir, repositorySlugs);
        }
        else
        {
            // Repo first (lowest priority), then global on top
            AddRepoLayers(builder, globalDir, repositorySlugs);
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

    private static void AddRepoLayers(
        IConfigurationBuilder builder,
        string globalDir,
        IEnumerable<string> repositorySlugs)
    {
        foreach (var slug in repositorySlugs)
        {
            var repoDir = Path.Combine(globalDir, slug);
            builder.AddJsonFile(
                Path.Combine(repoDir, ConfigFileName),
                optional: true,
                reloadOnChange: false);
        }
    }
}
