using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Writes shared resources configuration to disk.
/// </summary>
public sealed class SharedResourcesWriter : ISharedResourcesWriter
{
    private const string ConfigFileName = "aspire-shared-resources.json";
    private const string RepositoryConfigFileName = "settings.json";
    private const string RepositoryConfigFolder = ".aspire";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <inheritdoc />
    public async Task SaveGlobalAsync(GlobalSharedResources resources, CancellationToken cancellationToken = default)
    {
        var globalConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "spire",
            ConfigFileName);

        var directory = Path.GetDirectoryName(globalConfigPath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(resources, JsonOptions);
        await File.WriteAllTextAsync(globalConfigPath, json, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveRepositoryAsync(RepositorySharedResources resources, string repoPath, CancellationToken cancellationToken = default)
    {
        var configPath = Path.Combine(repoPath, RepositoryConfigFolder, RepositoryConfigFileName);

        var directory = Path.GetDirectoryName(configPath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(resources, JsonOptions);
        await File.WriteAllTextAsync(configPath, json, cancellationToken);
    }
}