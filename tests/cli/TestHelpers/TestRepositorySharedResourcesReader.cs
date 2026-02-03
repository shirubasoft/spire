using System.Text.Json;
using Spire.Cli.Services.Configuration;

namespace Spire.Cli.Tests.TestHelpers;

/// <summary>
/// A test implementation of IRepositorySharedResourcesReader that reads from disk.
/// </summary>
public sealed class TestRepositorySharedResourcesReader : IRepositorySharedResourcesReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public Task<RepositorySharedResources?> ReadAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        var path = GetSettingsFilePath(repositoryPath);
        if (!File.Exists(path))
        {
            return Task.FromResult<RepositorySharedResources?>(null);
        }

        var json = File.ReadAllText(path);
        var result = JsonSerializer.Deserialize<RepositorySharedResources>(json, SerializerOptions);
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public bool SettingsFileExists(string repositoryPath)
    {
        return File.Exists(GetSettingsFilePath(repositoryPath));
    }

    /// <inheritdoc/>
    public string GetSettingsFilePath(string repositoryPath)
    {
        return Path.Combine(repositoryPath, ".aspire", "settings.json");
    }
}
