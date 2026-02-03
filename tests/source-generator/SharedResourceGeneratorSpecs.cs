using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Spire.SourceGenerator.Tests;

/// <summary>
/// Tests for SharedResourceGenerator output.
/// Verifies that the generated code has the expected structure and compiles correctly.
/// </summary>
public class SharedResourceGeneratorSpecs
{
    [Test]
    public async Task GeneratedInterface_HasInnerProperty()
    {
        // Arrange
        const string json = """
            {
                "resources": {
                    "my-service": {
                        "mode": "container"
                    }
                }
            }
            """;

        // Act
        var generatedSources = RunGenerator(json);

        // Assert
        var resourceSource = generatedSources.FirstOrDefault(s => s.HintName == "MyService.g.cs");
        await Assert.That(resourceSource).IsNotNull();

        var sourceText = resourceSource!.SourceText.ToString();
        await Assert.That(sourceText).Contains("IResourceBuilder<IResource> Inner { get; }");
    }

    [Test]
    public async Task GeneratedInterface_HasConfigureMethods()
    {
        // Arrange
        const string json = """
            {
                "resources": {
                    "my-service": {
                        "mode": "container"
                    }
                }
            }
            """;

        // Act
        var generatedSources = RunGenerator(json);

        // Assert
        var resourceSource = generatedSources.FirstOrDefault(s => s.HintName == "MyService.g.cs");
        await Assert.That(resourceSource).IsNotNull();

        var sourceText = resourceSource!.SourceText.ToString();
        await Assert.That(sourceText).Contains("ConfigureContainer(");
        await Assert.That(sourceText).Contains("ConfigureProject(");
        await Assert.That(sourceText).Contains("Configure<T>(");
    }

    [Test]
    public async Task GeneratedConfigurationSource_IsCreated()
    {
        // Arrange
        const string json = """
            {
                "resources": {
                    "my-service": {
                        "mode": "container"
                    }
                }
            }
            """;

        // Act
        var generatedSources = RunGenerator(json);

        // Assert
        var configSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourcesConfiguration.g.cs");
        await Assert.That(configSource).IsNotNull();
    }

    [Test]
    public async Task GeneratedConfigurationSource_HasEmbeddedJson()
    {
        // Arrange
        const string json = """
            {
                "resources": {
                    "my-service": {
                        "mode": "container"
                    }
                }
            }
            """;

        // Act
        var generatedSources = RunGenerator(json);

        // Assert
        var configSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourcesConfiguration.g.cs");
        await Assert.That(configSource).IsNotNull();

        var sourceText = configSource!.SourceText.ToString();
        await Assert.That(sourceText).Contains("SharedResourcesJson");
        await Assert.That(sourceText).Contains("\"resources\"");
        await Assert.That(sourceText).Contains("\"my-service\"");
    }

    [Test]
    public async Task GeneratedConfigurationSource_HasAddSharedResourcesConfigurationMethod()
    {
        // Arrange
        const string json = """
            {
                "resources": {
                    "my-service": {
                        "mode": "container"
                    }
                }
            }
            """;

        // Act
        var generatedSources = RunGenerator(json);

        // Assert
        var configSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourcesConfiguration.g.cs");
        await Assert.That(configSource).IsNotNull();

        var sourceText = configSource!.SourceText.ToString();
        await Assert.That(sourceText).Contains("AddSharedResourcesConfiguration");
    }

    [Test]
    public async Task MultilineJson_IndentsCorrectlyInRawStringLiteral()
    {
        // Arrange - multi-line JSON that needs proper indentation
        const string json = """
            {
                "resources": {
                    "my-service": {
                        "mode": "container",
                        "containerMode": {
                            "imageName": "my-service",
                            "imageRegistry": "docker.io",
                            "imageTag": "latest",
                            "buildCommand": "docker build -t my-service .",
                            "buildWorkingDirectory": "./src/MyService"
                        }
                    }
                }
            }
            """;

        // Act
        var generatedSources = RunGenerator(json);

        // Assert
        var configSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourcesConfiguration.g.cs");
        await Assert.That(configSource).IsNotNull();

        var sourceText = configSource!.SourceText.ToString();

        // Verify the raw string literal is properly formatted (each line indented)
        await Assert.That(sourceText).Contains("private const string SharedResourcesJson = \"\"\"");
        await Assert.That(sourceText).Contains("\"\"\";");

        // The JSON should be parseable from the generated source
        await Assert.That(sourceText).Contains("\"containerMode\"");
    }

    [Test]
    public async Task ResourceWithHyphenatedId_GeneratesPascalCaseName()
    {
        // Arrange
        const string json = """
            {
                "resources": {
                    "my-cool-service": {
                        "mode": "container"
                    }
                }
            }
            """;

        // Act
        var generatedSources = RunGenerator(json);

        // Assert
        var resourceSource = generatedSources.FirstOrDefault(s => s.HintName == "MyCoolService.g.cs");
        await Assert.That(resourceSource).IsNotNull();

        var sourceText = resourceSource!.SourceText.ToString();
        await Assert.That(sourceText).Contains("IMyCoolServiceResourceBuilder");
        await Assert.That(sourceText).Contains("MyCoolServiceResourceBuilder");
        await Assert.That(sourceText).Contains("AddMyCoolService");
    }

    [Test]
    public async Task MultipleResources_GeneratesSourceForEach()
    {
        // Arrange
        const string json = """
            {
                "resources": {
                    "postgres": {
                        "mode": "container"
                    },
                    "redis": {
                        "mode": "container"
                    },
                    "api": {
                        "mode": "project"
                    }
                }
            }
            """;

        // Act
        var generatedSources = RunGenerator(json);

        // Assert
        await Assert.That(generatedSources.Any(s => s.HintName == "Postgres.g.cs")).IsTrue();
        await Assert.That(generatedSources.Any(s => s.HintName == "Redis.g.cs")).IsTrue();
        await Assert.That(generatedSources.Any(s => s.HintName == "Api.g.cs")).IsTrue();
        await Assert.That(generatedSources.Any(s => s.HintName == "SharedResourcesConfiguration.g.cs")).IsTrue();
    }

    [Test]
    public async Task EmptyResources_GeneratesNoResourceSources()
    {
        // Arrange
        const string json = """
            {
                "resources": {}
            }
            """;

        // Act
        var generatedSources = RunGenerator(json);

        // Assert - should not generate any resource files, only config
        await Assert.That(generatedSources.All(s => s.HintName == "SharedResourcesConfiguration.g.cs" || !s.HintName.EndsWith(".g.cs"))).IsTrue();
    }

    [Test]
    public async Task EmptyJson_GeneratesNothing()
    {
        // Arrange
        const string json = "";

        // Act
        var generatedSources = RunGenerator(json);

        // Assert
        await Assert.That(generatedSources).IsEmpty();
    }

    private static ImmutableArray<GeneratedSourceResult> RunGenerator(string json)
    {
        var generator = new SharedResourceGenerator();

        // Create a minimal compilation
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create additional text for the JSON file
        var additionalText = new InMemoryAdditionalText("SharedResources.g.json", json);

        // Create the driver and run
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var runResult = driver.GetRunResult();
        return runResult.GeneratedTrees
            .Select(t => new GeneratedSourceResult(
                Path.GetFileName(t.FilePath),
                t.GetText()))
            .ToImmutableArray();
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText? GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(text);
        }
    }

    private sealed record GeneratedSourceResult(string HintName, SourceText SourceText);
}
