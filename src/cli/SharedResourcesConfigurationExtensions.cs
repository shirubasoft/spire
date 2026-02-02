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
    /// <see cref="SharedResourcesConfiguration"/>. All paths are guaranteed absolute
    /// because the import step converts relative paths when writing to global config.
    /// Order (lowest to highest): global &lt; repository overrides &lt; env.
    /// </summary>
    /// <param name="repositorySlugs">Slugs identifying the Git repositories to load overrides for.</param>
    public static SharedResourcesConfiguration GetSharedResources(
        IEnumerable<string> repositorySlugs)
    {
        var globalDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "spire");

        var builder = new ConfigurationBuilder();

        // Layer 1 (lowest priority): global shared resources (absolute paths)
        builder.AddJsonFile(
            Path.Combine(globalDir, ConfigFileName),
            optional: true,
            reloadOnChange: false);

        // Layer 2: repository-scoped overrides (absolute paths, each repo layered in order)
        foreach (var slug in repositorySlugs)
        {
            var repoDir = Path.Combine(globalDir, slug);
            builder.AddJsonFile(
                Path.Combine(repoDir, ConfigFileName),
                optional: true,
                reloadOnChange: false);
        }

        // Layer 3 (highest priority): environment variables
        builder.AddEnvironmentVariables("ASPIRE_");

        var configuration = builder.Build();

        return configuration.Get<SharedResourcesConfiguration>()
            ?? new SharedResourcesConfiguration { Resources = new Dictionary<string, SharedResource>() };
    }
}
