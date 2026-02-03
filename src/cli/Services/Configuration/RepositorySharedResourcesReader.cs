using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Reads shared resources configuration from a repository's .aspire/settings.json file.
/// </summary>
public sealed class RepositorySharedResourcesReader : IRepositorySharedResourcesReader
{
    private const string SettingsDirectory = ".aspire";
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <inheritdoc />
    public async Task<RepositorySharedResources?> ReadAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        var settingsFilePath = GetSettingsFilePath(repositoryPath);

        if (!File.Exists(settingsFilePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(settingsFilePath, cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new RepositorySharedResources { Resources = [] };
        }

        // Parse as AspireSettings (with sharedResources wrapper) per schema
        var aspireSettings = JsonSerializer.Deserialize<AspireSettings>(json, JsonOptions);

        return aspireSettings?.SharedResources ?? new RepositorySharedResources { Resources = [] };
    }

    /// <inheritdoc />
    public bool SettingsFileExists(string repositoryPath)
    {
        return File.Exists(GetSettingsFilePath(repositoryPath));
    }

    /// <inheritdoc />
    public string GetSettingsFilePath(string repositoryPath)
    {
        return Path.Combine(repositoryPath, SettingsDirectory, SettingsFileName);
    }

    /// <summary>
    /// Represents the .aspire/settings.json file structure per schema.
    /// </summary>
    private sealed class AspireSettings
    {
        /// <summary>
        /// The path to the AppHost project file.
        /// </summary>
        public string? AppHostPath { get; init; }

        /// <summary>
        /// The shared resources configuration.
        /// </summary>
        public RepositorySharedResources? SharedResources { get; init; }
    }
}
