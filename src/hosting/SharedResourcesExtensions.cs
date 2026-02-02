using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding shared resource configuration.
/// </summary>
public static class SharedResourcesExtensions
{
    /// <summary>
    /// Adds shared resource configuration from the global config file.
    /// </summary>
    public static IConfigurationBuilder AddSharedResources(this IConfigurationBuilder builder)
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".spire",
            ".aspire-shared-resources.json");

        if (File.Exists(configPath))
        {
            builder.AddJsonFile(configPath, optional: true, reloadOnChange: false);
        }

        return builder;
    }
}
