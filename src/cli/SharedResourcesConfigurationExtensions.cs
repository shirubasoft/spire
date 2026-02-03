using Microsoft.Extensions.Configuration;

namespace Spire.Cli;

/// <summary>
/// Extension methods for loading shared resource configuration.
/// </summary>
public static class SharedResourcesConfigurationExtensions
{
    private const string ConfigFileName = "aspire-shared-resources.json";

    /// <summary>
    /// Loads shared resource configuration from the global config file and binds it to
    /// <see cref="GlobalSharedResources"/>. All paths are guaranteed absolute
    /// because the import step converts relative paths when writing to global config.
    /// </summary>
    public static GlobalSharedResources GetSharedResources()
    {
        var globalConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "spire",
            ConfigFileName);

        var builder = new ConfigurationBuilder()
            .AddJsonFile(globalConfigPath, optional: true, reloadOnChange: false);

        var configuration = builder.Build();

        return configuration.Get<GlobalSharedResources>()
            ?? GlobalSharedResources.Empty;
    }
}