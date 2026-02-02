using Microsoft.Extensions.Configuration;

namespace Spire.Cli;

/// <summary>
/// Extension methods for building shared resource configuration with layered priority.
/// </summary>
public static class SharedResourcesConfigurationExtensions
{
    private const string ConfigFileName = "aspire-shared-resources.json";

    /// <summary>
    /// Adds shared resource configuration with layered priority.
    /// Order (lowest to highest): global &lt; overrides:repo &lt; env.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="repositorySlugs">Slugs identifying the Git repositories to load overrides for.</param>
    public static IConfigurationBuilder AddSharedResources(
        this IConfigurationBuilder builder,
        IEnumerable<string> repositorySlugs)
    {
        var globalDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".spire");

        // Layer 1 (lowest priority): global shared resources
        builder.AddJsonFile(
            Path.Combine(globalDir, ConfigFileName),
            optional: true,
            reloadOnChange: false);

        // Layer 2: repository-scoped overrides (each repo layered in order)
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

        return builder;
    }
}
