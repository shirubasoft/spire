using Spire.Cli.Services.Analysis;

namespace Spire.Cli.Tests.Services.Analysis;

/// <summary>
/// Tests for project analyzer.
/// </summary>
public class ProjectAnalyzerSpecs
{
    private string _tempDir = null!;
    private ProjectAnalyzer _analyzer = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "project-analyzer-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _analyzer = new ProjectAnalyzer();
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Analyze_CsprojFile_ExtractsProjectName()
    {
        // Arrange
        var csprojPath = Path.Combine(_tempDir, "MyService.csproj");
        await File.WriteAllTextAsync(csprojPath, "<Project></Project>");

        // Act
        var result = await _analyzer.AnalyzeAsync(csprojPath);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ProjectName).IsEqualTo("MyService");
    }

    [Test]
    public async Task Analyze_CsprojFile_DeterminesProjectDirectory()
    {
        // Arrange
        var csprojPath = Path.Combine(_tempDir, "MyService.csproj");
        await File.WriteAllTextAsync(csprojPath, "<Project></Project>");

        // Act
        var result = await _analyzer.AnalyzeAsync(csprojPath);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ProjectDirectory).IsEqualTo(_tempDir);
    }

    [Test]
    public async Task Analyze_CsprojFile_BuildsContainerCommand()
    {
        // Arrange
        var csprojPath = Path.Combine(_tempDir, "MyService.csproj");
        await File.WriteAllTextAsync(csprojPath, "<Project></Project>");

        // Act
        var result = await _analyzer.AnalyzeAsync(csprojPath);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.BuildCommand).IsEqualTo("dotnet publish --os linux --arch x64 /t:PublishContainer");
    }

    [Test]
    public async Task Analyze_DirectoryWithCsproj_DetectsCsproj()
    {
        // Arrange
        var csprojPath = Path.Combine(_tempDir, "MyService.csproj");
        await File.WriteAllTextAsync(csprojPath, "<Project></Project>");

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempDir);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ProjectName).IsEqualTo("MyService");
    }

    [Test]
    public async Task Analyze_DirectoryWithoutCsproj_ReturnsNull()
    {
        // Arrange - empty directory

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempDir);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Analyze_NonCsprojFile_ReturnsNull()
    {
        // Arrange
        var txtPath = Path.Combine(_tempDir, "readme.txt");
        await File.WriteAllTextAsync(txtPath, "test");

        // Act
        var result = await _analyzer.AnalyzeAsync(txtPath);

        // Assert
        await Assert.That(result).IsNull();
    }
}
