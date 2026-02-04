using System.Text.Json;
using NSubstitute;
using Spectre.Console.Testing;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Tests.TestHelpers;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Integration;

/// <summary>
/// Integration tests for the resource remove command.
/// </summary>
public sealed class ResourceRemoveIntegrationSpecs
{
    private readonly string _testDir;
    private readonly string _globalConfigPath;
    private readonly string _repoPath;
    private readonly string _repoSettingsPath;

    public ResourceRemoveIntegrationSpecs()
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
    public async Task Remove_ExistingResource_RemovesFromGlobal()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis");
        await WriteGlobalConfig(globalResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(ReadGlobalConfig()!));

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync("postgres", yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        var afterResources = ReadGlobalConfig();
        await Assert.That(afterResources!.ContainsResource("postgres")).IsFalse();
        await Assert.That(afterResources.ContainsResource("redis")).IsTrue();
    }

    [Test]
    public async Task Remove_ResourceInBothLocations_RemovesBoth()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis");
        var repoResources = ResourceTestHelpers.CreateRepoResources("postgres");
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

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(ReadGlobalConfig()!));

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync("postgres", yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);

        var afterGlobal = ReadGlobalConfig();
        await Assert.That(afterGlobal!.ContainsResource("postgres")).IsFalse();

        var afterRepo = ReadRepoConfig();
        await Assert.That(afterRepo!.ContainsResource("postgres")).IsFalse();
    }

    [Test]
    public async Task Remove_ResourceOnlyInGlobal_RemovesGlobalOnly()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis");
        var repoResources = ResourceTestHelpers.CreateRepoResources("redis");
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

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(ReadGlobalConfig()!));

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync("postgres", yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);

        var afterGlobal = ReadGlobalConfig();
        await Assert.That(afterGlobal!.ContainsResource("postgres")).IsFalse();

        // Repo should still have redis
        var afterRepo = ReadRepoConfig();
        await Assert.That(afterRepo!.ContainsResource("redis")).IsTrue();
    }

    [Test]
    public async Task Remove_NonExistingResource_ReturnsError()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres");
        await WriteGlobalConfig(globalResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(ReadGlobalConfig()!));

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync("nonexistent", yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("not found");
    }

    [Test]
    public async Task Remove_WithYes_NoPrompt()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres");
        await WriteGlobalConfig(globalResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(ReadGlobalConfig()!));

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync("postgres", yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).DoesNotContain("Continue?");
    }

    [Test]
    public async Task Remove_VerifyJsonStateBeforeAndAfter_GlobalConfig()
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

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(ReadGlobalConfig()!));

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        await handler.ExecuteAsync("postgres", yes: true);

        // Verify after state
        var afterJson = await File.ReadAllTextAsync(_globalConfigPath);
        await Assert.That(afterJson).DoesNotContain("\"postgres\"");
        await Assert.That(afterJson).Contains("redis");
    }

    [Test]
    public async Task Remove_VerifyJsonStateBeforeAndAfter_RepositorySettings()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres");
        var repoResources = ResourceTestHelpers.CreateRepoResources("postgres", "redis");
        await WriteGlobalConfig(globalResources);
        await WriteRepoConfig(repoResources);

        // Verify before state
        var beforeJson = await File.ReadAllTextAsync(_repoSettingsPath);
        await Assert.That(beforeJson).Contains("postgres");

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

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(ReadGlobalConfig()!));

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        await handler.ExecuteAsync("postgres", yes: true);

        // Verify after state
        var afterJson = await File.ReadAllTextAsync(_repoSettingsPath);
        await Assert.That(afterJson).DoesNotContain("\"postgres\"");
        await Assert.That(afterJson).Contains("redis");
    }

    [Test]
    public async Task Remove_MultipleResources_PreservesOthersInJson()
    {
        // Arrange
        var globalResources = ResourceTestHelpers.CreateGlobalResources("postgres", "redis", "mongo");
        await WriteGlobalConfig(globalResources);

        var console = new TestConsole();
        var writer = CreateWriter();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(ReadGlobalConfig()!));

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        await handler.ExecuteAsync("postgres", yes: true);

        // Assert
        var afterResources = ReadGlobalConfig();
        await Assert.That(afterResources!.Count).IsEqualTo(2);
        await Assert.That(afterResources.ContainsResource("redis")).IsTrue();
        await Assert.That(afterResources.ContainsResource("mongo")).IsTrue();
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
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo => Task.FromResult(ReadRepoConfig()));
        reader.SettingsFileExists(Arg.Any<string>()).Returns(true);
        reader.GetSettingsFilePath(Arg.Any<string>()).Returns(_repoSettingsPath);
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
