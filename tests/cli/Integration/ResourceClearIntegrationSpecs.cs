using System.Text.Json;
using NSubstitute;
using Spectre.Console.Testing;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Tests.TestHelpers;

namespace Spire.Cli.Tests.Integration;

/// <summary>
/// Integration tests for the resource clear command.
/// </summary>
public sealed class ResourceClearIntegrationSpecs
{
    private readonly string _testDir;
    private readonly string _globalConfigPath;
    private readonly string _repoPath;
    private readonly string _repoSettingsPath;

    public ResourceClearIntegrationSpecs()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"spire-test-{Guid.NewGuid()}");
        _globalConfigPath = Path.Combine(_testDir, "global", "aspire-shared-resources.json");
        _repoPath = Path.Combine(_testDir, "repo");
        _repoSettingsPath = Path.Combine(_repoPath, ".aspire", "settings.json");

        Directory.CreateDirectory(Path.GetDirectoryName(_globalConfigPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(_repoSettingsPath)!);
    }

    [After(Test)]
    public Task Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
        return Task.CompletedTask;
    }

    [Test]
    public async Task Clear_All_EmptiesGlobalConfig()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis", "mongo");
        await WriteGlobalConfig(globalResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            () => ReadGlobalConfig()!);

        // Act
        var result = await handler.ExecuteAsync(null, includeRepo: false, yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        var afterResources = ReadGlobalConfig();
        await Assert.That(afterResources!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_SpecificIds_RemovesOnlyThose()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis", "mongo");
        await WriteGlobalConfig(globalResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            () => ReadGlobalConfig()!);

        // Act
        var result = await handler.ExecuteAsync(["postgres", "redis"], includeRepo: false, yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        var afterResources = ReadGlobalConfig();
        await Assert.That(afterResources!.Count).IsEqualTo(1);
        await Assert.That(afterResources.ContainsResource("mongo")).IsTrue();
    }

    [Test]
    public async Task Clear_WithIncludeRepo_ClearsBoth()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis");
        var repoResources = ResourceTestHelpers.CreateRepoResources("postgres", "mongo");
        await WriteGlobalConfig(globalResources);
        await WriteRepoConfig(repoResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = CreateRepoReader();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(true);
        gitService.GetRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = _repoPath,
                CurrentBranch = "main",
                LatestCommitHash = "abc123",
                IsDirty = false
            });

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            () => ReadGlobalConfig()!);

        // Act
        var result = await handler.ExecuteAsync(null, includeRepo: true, yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);

        var afterGlobal = ReadGlobalConfig();
        await Assert.That(afterGlobal!.Count).IsEqualTo(0);

        var afterRepo = ReadRepoConfig();
        await Assert.That(afterRepo!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_WithoutIncludeRepo_PreservesRepo()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis");
        var repoResources = ResourceTestHelpers.CreateRepoResources("postgres", "mongo");
        await WriteGlobalConfig(globalResources);
        await WriteRepoConfig(repoResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            () => ReadGlobalConfig()!);

        // Act
        var result = await handler.ExecuteAsync(null, includeRepo: false, yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);

        var afterGlobal = ReadGlobalConfig();
        await Assert.That(afterGlobal!.Count).IsEqualTo(0);

        // Repo should be unchanged
        var afterRepo = ReadRepoConfig();
        await Assert.That(afterRepo!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Clear_AlreadyEmpty_Succeeds()
    {
        // Arrange
        var globalResources = GlobalSharedResources.Empty;
        await WriteGlobalConfig(globalResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            () => ReadGlobalConfig()!);

        // Act
        var result = await handler.ExecuteAsync(null, includeRepo: false, yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_WithYes_NoPrompt()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres");
        await WriteGlobalConfig(globalResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            () => ReadGlobalConfig()!);

        // Act
        var result = await handler.ExecuteAsync(null, includeRepo: false, yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).DoesNotContain("Continue?");
    }

    [Test]
    public async Task ClearAll_VerifyJsonStateBeforeAndAfter_GlobalConfig()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis");
        await WriteGlobalConfig(globalResources);

        // Verify before state
        var beforeJson = await File.ReadAllTextAsync(_globalConfigPath);
        await Assert.That(beforeJson).Contains("postgres");
        await Assert.That(beforeJson).Contains("redis");

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            () => ReadGlobalConfig()!);

        // Act
        await handler.ExecuteAsync(null, includeRepo: false, yes: true);

        // Verify after state - should be empty but valid JSON
        var afterJson = await File.ReadAllTextAsync(_globalConfigPath);
        await Assert.That(afterJson).DoesNotContain("\"postgres\"");
        await Assert.That(afterJson).DoesNotContain("\"redis\"");

        var afterResources = ReadGlobalConfig();
        await Assert.That(afterResources!.Resources).IsNotNull();
    }

    [Test]
    public async Task ClearAll_VerifyJsonStateBeforeAndAfter_RepositorySettings()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres");
        var repoResources = ResourceTestHelpers.CreateRepoResources("postgres", "redis");
        await WriteGlobalConfig(globalResources);
        await WriteRepoConfig(repoResources);

        // Verify before state
        var beforeJson = await File.ReadAllTextAsync(_repoSettingsPath);
        await Assert.That(beforeJson).Contains("postgres");
        await Assert.That(beforeJson).Contains("redis");

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = CreateRepoReader();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(true);
        gitService.GetRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = _repoPath,
                CurrentBranch = "main",
                LatestCommitHash = "abc123",
                IsDirty = false
            });

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            () => ReadGlobalConfig()!);

        // Act
        await handler.ExecuteAsync(null, includeRepo: true, yes: true);

        // Verify after state
        var afterJson = await File.ReadAllTextAsync(_repoSettingsPath);
        await Assert.That(afterJson).DoesNotContain("\"postgres\"");
        await Assert.That(afterJson).DoesNotContain("\"redis\"");
    }

    [Test]
    public async Task ClearSpecific_VerifyJsonStateBeforeAndAfter()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis", "mongo", "mysql", "elasticsearch");
        await WriteGlobalConfig(globalResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            () => ReadGlobalConfig()!);

        // Act
        await handler.ExecuteAsync(["postgres", "redis"], includeRepo: false, yes: true);

        // Verify after state
        var afterResources = ReadGlobalConfig();
        await Assert.That(afterResources!.Count).IsEqualTo(3);
        await Assert.That(afterResources.ContainsResource("mongo")).IsTrue();
        await Assert.That(afterResources.ContainsResource("mysql")).IsTrue();
        await Assert.That(afterResources.ContainsResource("elasticsearch")).IsTrue();
    }

    [Test]
    public async Task Clear_WithIncludeRepo_VerifyBothJsonFilesCleared()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis");
        var repoResources = ResourceTestHelpers.CreateRepoResources("postgres", "mongo");
        await WriteGlobalConfig(globalResources);
        await WriteRepoConfig(repoResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = CreateRepoReader();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(true);
        gitService.GetRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = _repoPath,
                CurrentBranch = "main",
                LatestCommitHash = "abc123",
                IsDirty = false
            });

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            () => ReadGlobalConfig()!);

        // Act
        await handler.ExecuteAsync(null, includeRepo: true, yes: true);

        // Verify both files have empty resources
        var afterGlobal = ReadGlobalConfig();
        var afterRepo = ReadRepoConfig();

        await Assert.That(afterGlobal!.Resources).Count().IsEqualTo(0);
        await Assert.That(afterRepo!.Resources).Count().IsEqualTo(0);
    }

    private ISharedResourcesWriter CreateWriter()
    {
        var writer = Substitute.For<ISharedResourcesWriter>();
        writer.SaveGlobalAsync(Arg.Any<GlobalSharedResources>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var resources = callInfo.Arg<GlobalSharedResources>();
                await WriteGlobalConfig(resources);
            });
        writer.SaveRepositoryAsync(Arg.Any<RepositorySharedResources>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var resources = callInfo.Arg<RepositorySharedResources>();
                await WriteRepoConfig(resources);
            });
        return writer;
    }

    private IRepositorySharedResourcesReader CreateRepoReader()
    {
        var reader = Substitute.For<IRepositorySharedResourcesReader>();
        reader.Read(Arg.Any<string>()).Returns(callInfo => ReadRepoConfig());
        reader.Exists(Arg.Any<string>()).Returns(true);
        reader.GetSettingsPath(Arg.Any<string>()).Returns(_repoSettingsPath);
        return reader;
    }

    private async Task WriteGlobalConfig(GlobalSharedResources resources)
    {
        var json = JsonSerializer.Serialize(resources, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(_globalConfigPath, json);
    }

    private async Task WriteRepoConfig(RepositorySharedResources resources)
    {
        var json = JsonSerializer.Serialize(resources, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(_repoSettingsPath, json);
    }

    private GlobalSharedResources? ReadGlobalConfig()
    {
        if (!File.Exists(_globalConfigPath)) return null;
        var json = File.ReadAllText(_globalConfigPath);
        return JsonSerializer.Deserialize<GlobalSharedResources>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private RepositorySharedResources? ReadRepoConfig()
    {
        if (!File.Exists(_repoSettingsPath)) return null;
        var json = File.ReadAllText(_repoSettingsPath);
        return JsonSerializer.Deserialize<RepositorySharedResources>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
