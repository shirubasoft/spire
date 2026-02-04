using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;
using Spire.Cli.Tests.TestHelpers;

namespace Spire.Cli.Tests;

/// <summary>
/// Tests for GlobalSharedResourcesReader when the config file does not exist.
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
        var gitService = Substitute.For<IGitService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var reader = new GlobalSharedResourcesReader(gitService, tagGenerator);

        var result = await reader.GetSharedResourcesAsync();

        await Assert.That(result.Resources).IsNotNull();
        await Assert.That(result.Resources).IsEmpty();
    }
}

/// <summary>
/// Tests for GlobalSharedResourcesReader when the config file is empty or has empty JSON.
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
        var gitService = Substitute.For<IGitService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var reader = new GlobalSharedResourcesReader(gitService, tagGenerator);

        var result = await reader.GetSharedResourcesAsync();

        await Assert.That(result.Resources).IsNotNull();
        await Assert.That(result.Resources).IsEmpty();
    }
}

/// <summary>
/// Tests for GlobalSharedResourcesReader when the config file has resources.
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
        var gitService = Substitute.For<IGitService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var reader = new GlobalSharedResourcesReader(gitService, tagGenerator);

        var result = await reader.GetSharedResourcesAsync();

        await Assert.That(result.Resources.Count).IsEqualTo(2);
        await Assert.That(result.Resources.ContainsKey("postgres")).IsTrue();
        await Assert.That(result.Resources.ContainsKey("redis")).IsTrue();
    }

    [Test]
    public async Task GetSharedResources_WhenFileHasResources_DeserializesCorrectly()
    {
        var gitService = Substitute.For<IGitService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var reader = new GlobalSharedResourcesReader(gitService, tagGenerator);

        var result = await reader.GetSharedResourcesAsync();

        var postgres = result.Resources["postgres"];
        await Assert.That(postgres.Mode).IsEqualTo(Mode.Container);
        await Assert.That(postgres.ContainerMode).IsNotNull();
        await Assert.That(postgres.ContainerMode!.ImageName).IsEqualTo("postgres");
    }

    [Test]
    public async Task GetSharedResources_WhenGitAvailable_ResolvesImageTagFromBranchTag()
    {
        var gitService = Substitute.For<IGitService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var reader = new GlobalSharedResourcesReader(gitService, tagGenerator);

        gitService.GetRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
                CurrentBranch = "feature/my-branch",
                LatestCommitHash = "abc1234def",
                IsDirty = false
            });

        tagGenerator.Generate(Arg.Any<GitRepository>())
            .Returns(new ImageTags
            {
                CommitTag = "abc1234",
                BranchTag = "feature-my-branch"
            });

        var result = await reader.GetSharedResourcesAsync();

        var postgres = result.Resources["postgres"];
        await Assert.That(postgres.ContainerMode!.ImageTag).IsEqualTo("feature-my-branch");
    }

    [Test]
    public async Task GetSharedResources_WhenGitUnavailable_KeepsOriginalImageTag()
    {
        var gitService = Substitute.For<IGitService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var reader = new GlobalSharedResourcesReader(gitService, tagGenerator);

        gitService.GetRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Git not available"));

        var result = await reader.GetSharedResourcesAsync();

        var postgres = result.Resources["postgres"];
        await Assert.That(postgres.ContainerMode!.ImageTag).IsEqualTo("latest");
    }
}

/// <summary>
/// Tests for GlobalSharedResourcesReader when the config file has invalid JSON.
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
        var gitService = Substitute.For<IGitService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var reader = new GlobalSharedResourcesReader(gitService, tagGenerator);

        await Assert.That(async () => await reader.GetSharedResourcesAsync()).Throws<Exception>();
    }
}
