using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spire.Cli.Tests.TestHelpers;

/// <summary>
/// Provides helper methods for setting up test configuration files.
/// </summary>
public static class ConfigurationTestHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Gets the path to the global configuration directory.
    /// </summary>
    public static string GetGlobalConfigDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "spire");
    }

    /// <summary>
    /// Gets the path to the global shared resources configuration file.
    /// </summary>
    public static string GetGlobalConfigFilePath()
    {
        return Path.Combine(GetGlobalConfigDirectory(), "aspire-shared-resources.json");
    }

    /// <summary>
    /// Gets the path to the backup configuration file.
    /// </summary>
    public static string GetBackupConfigFilePath()
    {
        return GetGlobalConfigFilePath() + ".backup";
    }

    /// <summary>
    /// Creates a sample shared resource for testing.
    /// </summary>
    public static SharedResource CreateSampleResource(string imageName = "postgres")
    {
        return new SharedResource
        {
            Mode = Mode.Container,
            ContainerMode = new ContainerModeSettings
            {
                ImageName = imageName,
                ImageRegistry = "docker.io",
                ImageTag = "latest",
                BuildCommand = $"docker build -t {imageName} .",
                BuildWorkingDirectory = $"/home/user/projects/{imageName}"
            },
            ProjectMode = null,
            GitRepository = new GitRepositorySettings
            {
                Url = new Uri($"https://github.com/org/{imageName}"),
                DefaultBranch = "main"
            }
        };
    }

    /// <summary>
    /// Creates a global shared resources configuration with the specified resources.
    /// </summary>
    public static GlobalSharedResources CreateConfig(params (string Id, SharedResource Resource)[] resources)
    {
        return new GlobalSharedResources
        {
            Resources = resources.ToDictionary(r => r.Id, r => r.Resource)
        };
    }

    /// <summary>
    /// Writes a global shared resources configuration to the config file.
    /// </summary>
    public static void WriteConfig(GlobalSharedResources config)
    {
        var directory = GetGlobalConfigDirectory();
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(GetGlobalConfigFilePath(), json);
    }

    /// <summary>
    /// Deletes the global config file if it exists.
    /// </summary>
    public static void DeleteConfig()
    {
        var filePath = GetGlobalConfigFilePath();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Backs up the existing config file if it exists.
    /// </summary>
    /// <returns>True if a backup was created; false otherwise.</returns>
    public static bool BackupConfig()
    {
        var filePath = GetGlobalConfigFilePath();
        var backupPath = GetBackupConfigFilePath();

        if (File.Exists(filePath))
        {
            File.Copy(filePath, backupPath, overwrite: true);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Restores the config file from backup, or deletes it if no backup existed.
    /// </summary>
    /// <param name="hadBackup">Whether a backup existed before the test.</param>
    public static void RestoreConfig(bool hadBackup)
    {
        var filePath = GetGlobalConfigFilePath();
        var backupPath = GetBackupConfigFilePath();

        if (hadBackup)
        {
            File.Copy(backupPath, filePath, overwrite: true);
            File.Delete(backupPath);
        }
        else if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
