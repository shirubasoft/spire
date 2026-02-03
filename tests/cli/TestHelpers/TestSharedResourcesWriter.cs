using System.Text.Json;
using Spire.Cli.Services.Configuration;

namespace Spire.Cli.Tests.TestHelpers;

/// <summary>
/// A test implementation of ISharedResourcesWriter that writes to disk.
/// </summary>
public sealed class TestSharedResourcesWriter : ISharedResourcesWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public async Task SaveGlobalAsync(GlobalSharedResources resources, CancellationToken cancellationToken = default)
    {
        var path = GetGlobalConfigPath();
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(resources, SerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SaveRepositoryAsync(RepositorySharedResources resources, string repoPath, CancellationToken cancellationToken = default)
    {
        var path = GetRepositorySettingsPath(repoPath);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(resources, SerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    /// <summary>
    /// Gets the path to the global config file.
    /// </summary>
    public static string GetGlobalConfigPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "spire",
            "aspire-shared-resources.json");
    }

    /// <summary>
    /// Gets the path to the repository settings file.
    /// </summary>
    public static string GetRepositorySettingsPath(string repoPath)
    {
        return Path.Combine(repoPath, ".aspire", "settings.json");
    }
}
