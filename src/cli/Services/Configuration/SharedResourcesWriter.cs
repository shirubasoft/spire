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

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
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

        var json = JsonSerializer.Serialize(resources, WriteOptions);
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

        // Read existing settings to preserve appHostPath
        string? existingAppHostPath = null;
        if (File.Exists(settingsPath))
        {
            var existingJson = await File.ReadAllTextAsync(settingsPath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(existingJson))
            {
                var existingSettings = JsonSerializer.Deserialize<AspireSettings>(existingJson, ReadOptions);
                existingAppHostPath = existingSettings?.AppHostPath;
            }
        }

        // Wrap in AspireSettings structure per schema
        var aspireSettings = new AspireSettings
        {
            AppHostPath = existingAppHostPath,
            SharedResources = resources
        };

        var json = JsonSerializer.Serialize(aspireSettings, WriteOptions);
        await File.WriteAllTextAsync(settingsPath, json, cancellationToken);
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
