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

        return JsonSerializer.Deserialize<RepositorySharedResources>(json, JsonOptions)
            ?? new RepositorySharedResources { Resources = [] };
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
}
