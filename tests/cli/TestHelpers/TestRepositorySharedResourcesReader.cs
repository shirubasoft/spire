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
    public RepositorySharedResources? Read(string repoPath)
    {
        var path = GetSettingsPath(repoPath);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<RepositorySharedResources>(json, SerializerOptions);
    }

    /// <inheritdoc/>
    public bool Exists(string repoPath)
    {
        return File.Exists(GetSettingsPath(repoPath));
    }

    /// <inheritdoc/>
    public string GetSettingsPath(string repoPath)
    {
        return Path.Combine(repoPath, ".aspire", "settings.json");
    }
}
