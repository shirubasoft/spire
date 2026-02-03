using Spire.Cli.Tests.TestHelpers;

namespace Spire.Cli.Tests;

/// <summary>
/// Tests for GetSharedResources when the config file does not exist.
/// </summary>
[NotInParallel("ConfigFile")]
public class WhenConfigFileDoesNotExistSpecs
{
    private bool _hadBackup;

    [Before(Test)]
    public void Setup()
    {
        _hadBackup = ConfigurationTestHelper.BackupConfig();
        ConfigurationTestHelper.DeleteConfig();
    }

    [After(Test)]
    public void Cleanup()
    {
        ConfigurationTestHelper.RestoreConfig(_hadBackup);
    }

    [Test]
    public async Task GetSharedResources_WhenFileDoesNotExist_ReturnsEmpty()
    {
        var result = SharedResourcesConfigurationExtensions.GetSharedResources();

        await Assert.That(result.Resources).IsNotNull();
        await Assert.That(result.Resources).IsEmpty();
    }
}

/// <summary>
/// Tests for GetSharedResources when the config file is empty or has empty JSON.
/// </summary>
[NotInParallel("ConfigFile")]
public class WhenConfigFileIsEmptySpecs
{
    private bool _hadBackup;

    [Before(Test)]
    public void Setup()
    {
        _hadBackup = ConfigurationTestHelper.BackupConfig();

        var directory = ConfigurationTestHelper.GetGlobalConfigDirectory();
        Directory.CreateDirectory(directory);
        File.WriteAllText(ConfigurationTestHelper.GetGlobalConfigFilePath(), "{}");
    }

    [After(Test)]
    public void Cleanup()
    {
        ConfigurationTestHelper.RestoreConfig(_hadBackup);
    }

    [Test]
    public async Task GetSharedResources_WhenFileIsEmpty_ReturnsEmpty()
    {
        var result = SharedResourcesConfigurationExtensions.GetSharedResources();

        await Assert.That(result.Resources).IsNotNull();
        await Assert.That(result.Resources).IsEmpty();
    }
}

/// <summary>
/// Tests for GetSharedResources when the config file has resources.
/// </summary>
[NotInParallel("ConfigFile")]
public class WhenConfigFileHasResourcesSpecs
{
    private bool _hadBackup;

    [Before(Test)]
    public void Setup()
    {
        _hadBackup = ConfigurationTestHelper.BackupConfig();

        var resources = ConfigurationTestHelper.CreateConfig(
            ("postgres", ConfigurationTestHelper.CreateSampleResource("postgres")),
            ("redis", ConfigurationTestHelper.CreateSampleResource("redis"))
        );
        ConfigurationTestHelper.WriteConfig(resources);
    }

    [After(Test)]
    public void Cleanup()
    {
        ConfigurationTestHelper.RestoreConfig(_hadBackup);
    }

    [Test]
    public async Task GetSharedResources_WhenFileHasResources_ReturnsAll()
    {
        var result = SharedResourcesConfigurationExtensions.GetSharedResources();

        await Assert.That(result.Resources.Count).IsEqualTo(2);
        await Assert.That(result.Resources.ContainsKey("postgres")).IsTrue();
        await Assert.That(result.Resources.ContainsKey("redis")).IsTrue();
    }

    [Test]
    public async Task GetSharedResources_WhenFileHasResources_DeserializesCorrectly()
    {
        var result = SharedResourcesConfigurationExtensions.GetSharedResources();

        var postgres = result.Resources["postgres"];
        await Assert.That(postgres.Mode).IsEqualTo(Mode.Container);
        await Assert.That(postgres.ContainerMode).IsNotNull();
        await Assert.That(postgres.ContainerMode!.ImageName).IsEqualTo("postgres");
    }
}

/// <summary>
/// Tests for GetSharedResources when the config file has invalid JSON.
/// </summary>
[NotInParallel("ConfigFile")]
public class WhenConfigFileHasInvalidJsonSpecs
{
    private bool _hadBackup;

    [Before(Test)]
    public void Setup()
    {
        _hadBackup = ConfigurationTestHelper.BackupConfig();

        var directory = ConfigurationTestHelper.GetGlobalConfigDirectory();
        Directory.CreateDirectory(directory);
        File.WriteAllText(ConfigurationTestHelper.GetGlobalConfigFilePath(), "{ invalid json }");
    }

    [After(Test)]
    public void Cleanup()
    {
        ConfigurationTestHelper.RestoreConfig(_hadBackup);
    }

    [Test]
    public async Task GetSharedResources_WhenFileHasInvalidJson_ThrowsException()
    {
        await Assert.That(() => SharedResourcesConfigurationExtensions.GetSharedResources()).Throws<Exception>();
    }
}