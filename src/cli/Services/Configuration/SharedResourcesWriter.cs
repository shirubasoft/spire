using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Writes shared resources configuration to disk.
/// </summary>
public sealed class SharedResourcesWriter : ISharedResourcesWriter
{
    private const string GlobalConfigDirectory = ".aspire/spire";
    private const string GlobalConfigFileName = "aspire-shared-resources.json";
    private const string RepositorySettingsDirectory = ".aspire";
    private const string RepositorySettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc />
    public async Task SaveGlobalAsync(GlobalSharedResources resources, CancellationToken cancellationToken = default)
    {
        var configPath = GetGlobalConfigPath();
        var directory = Path.GetDirectoryName(configPath)!;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(resources, JsonOptions);
        await File.WriteAllTextAsync(configPath, json, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveRepositoryAsync(RepositorySharedResources resources, string repoPath, CancellationToken cancellationToken = default)
    {
        var settingsPath = GetRepositorySettingsPath(repoPath);
        var directory = Path.GetDirectoryName(settingsPath)!;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(resources, JsonOptions);
        await File.WriteAllTextAsync(settingsPath, json, cancellationToken);
    }

    /// <summary>
    /// Gets the path to the global configuration file.
    /// </summary>
    public static string GetGlobalConfigPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, GlobalConfigDirectory, GlobalConfigFileName);
    }

    /// <summary>
    /// Gets the path to the repository settings file.
    /// </summary>
    /// <param name="repoPath">The repository root path.</param>
    public static string GetRepositorySettingsPath(string repoPath)
    {
        return Path.Combine(repoPath, RepositorySettingsDirectory, RepositorySettingsFileName);
    }
}
