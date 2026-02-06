using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Spire.SourceGenerator.Tests;

public class FluentChainGeneratesInterceptorsSpecs
{
    [Test]
    public async Task FluentChain_WithHttpEndpoint_GeneratesInterceptor()
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

        const string appHostCode = """
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            public static class Program
            {
                public static void Main()
                {
                    var builder = default(IDistributedApplicationBuilder)!;
                    builder.AddMyService().WithHttpEndpoint(targetPort: 8080);
                }
            }
            """;

        // Act
        var generatedSources = InterceptorGeneratorTestHelper.RunInterceptorGenerator(json, appHostCode);

        // Assert
        var interceptorSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourceInterceptors.g.cs");
        await Assert.That(interceptorSource).IsNotNull();

        var sourceText = interceptorSource!.SourceText.ToString();
        await Assert.That(sourceText).Contains("WithHttpEndpoint");
        await Assert.That(sourceText).Contains("InterceptsLocation");
    }

    [Test]
    public async Task FluentChain_MultipleCallsToSameMethod_GroupedByMethod()
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

        const string appHostCode = """
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            public static class Program
            {
                public static void Main()
                {
                    var builder = default(IDistributedApplicationBuilder)!;
                    builder.AddMyService()
                        .WithHttpEndpoint(targetPort: 8080)
                        .WithHttpEndpoint(targetPort: 9090);
                }
            }
            """;

        // Act
        var generatedSources = InterceptorGeneratorTestHelper.RunInterceptorGenerator(json, appHostCode);

        // Assert
        var interceptorSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourceInterceptors.g.cs");
        await Assert.That(interceptorSource).IsNotNull();

        var sourceText = interceptorSource!.SourceText.ToString();

        // Count InterceptsLocation attributes — should have 2 for the 2 call sites
        var locationCount = sourceText.Split("InterceptsLocation").Length - 1;
        await Assert.That(locationCount).IsGreaterThanOrEqualTo(2);
    }
}

public class NonSharedResourceChainNotInterceptedSpecs
{
    [Test]
    public async Task RegularAspireResource_NoInterceptorsGenerated()
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

        // Code that uses a regular container resource, NOT a SharedResource
        const string appHostCode = """
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            public static class Program
            {
                public static void Main()
                {
                    var builder = default(IDistributedApplicationBuilder)!;
                    builder.AddContainer("redis", "redis:latest").WithHttpEndpoint(targetPort: 6379);
                }
            }
            """;

        // Act
        var generatedSources = InterceptorGeneratorTestHelper.RunInterceptorGenerator(json, appHostCode);

        // Assert — no interceptor file should be generated
        var interceptorSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourceInterceptors.g.cs");
        await Assert.That(interceptorSource).IsNull();
    }
}

public class ConfigureMethodsNotInterceptedSpecs
{
    [Test]
    public async Task ConfigureContainer_NotIntercepted()
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

        const string appHostCode = """
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            public static class Program
            {
                public static void Main()
                {
                    var builder = default(IDistributedApplicationBuilder)!;
                    builder.AddMyService().ConfigureContainer(b => { });
                }
            }
            """;

        // Act
        var generatedSources = InterceptorGeneratorTestHelper.RunInterceptorGenerator(json, appHostCode);

        // Assert
        var interceptorSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourceInterceptors.g.cs");

        // Either no interceptor file or it doesn't contain ConfigureContainer
        if (interceptorSource is not null)
        {
            var sourceText = interceptorSource.SourceText.ToString();
            await Assert.That(sourceText).DoesNotContain("ConfigureContainer");
        }
    }

    [Test]
    public async Task ConfigureProject_NotIntercepted()
    {
        // Arrange
        const string json = """
            {
                "resources": {
                    "my-service": {
                        "mode": "project"
                    }
                }
            }
            """;

        const string appHostCode = """
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            public static class Program
            {
                public static void Main()
                {
                    var builder = default(IDistributedApplicationBuilder)!;
                    builder.AddMyService().ConfigureProject(b => { });
                }
            }
            """;

        // Act
        var generatedSources = InterceptorGeneratorTestHelper.RunInterceptorGenerator(json, appHostCode);

        // Assert
        var interceptorSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourceInterceptors.g.cs");

        if (interceptorSource is not null)
        {
            var sourceText = interceptorSource.SourceText.ToString();
            await Assert.That(sourceText).DoesNotContain("ConfigureProject");
        }
    }
}

public class VariableAssignmentTrackedSpecs
{
    [Test]
    public async Task VariableAssignment_TrackedToAddCall()
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

        const string appHostCode = """
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            public static class Program
            {
                public static void Main()
                {
                    var builder = default(IDistributedApplicationBuilder)!;
                    var svc = builder.AddMyService();
                    svc.WithHttpEndpoint(targetPort: 8080);
                }
            }
            """;

        // Act
        var generatedSources = InterceptorGeneratorTestHelper.RunInterceptorGenerator(json, appHostCode);

        // Assert
        var interceptorSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourceInterceptors.g.cs");
        await Assert.That(interceptorSource).IsNotNull();

        var sourceText = interceptorSource!.SourceText.ToString();
        await Assert.That(sourceText).Contains("WithHttpEndpoint");
    }
}

public class InterceptorForwardsToInnerBuilderSpecs
{
    [Test]
    public async Task GeneratedInterceptor_ForwardsToBothModes()
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

        const string appHostCode = """
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            public static class Program
            {
                public static void Main()
                {
                    var builder = default(IDistributedApplicationBuilder)!;
                    builder.AddMyService().WithHttpEndpoint(targetPort: 8080);
                }
            }
            """;

        // Act
        var generatedSources = InterceptorGeneratorTestHelper.RunInterceptorGenerator(json, appHostCode);

        // Assert
        var interceptorSource = generatedSources.FirstOrDefault(s => s.HintName == "SharedResourceInterceptors.g.cs");
        await Assert.That(interceptorSource).IsNotNull();

        var sourceText = interceptorSource!.SourceText.ToString();

        // Verify forwarding pattern
        await Assert.That(sourceText).Contains("resource.InnerBuilder");
        await Assert.That(sourceText).Contains("ResourceMode.Container");
        await Assert.That(sourceText).Contains("ContainerResource");
        await Assert.That(sourceText).Contains("ProjectResource");
        await Assert.That(sourceText).Contains("return builder;");
    }
}

internal static class InterceptorGeneratorTestHelper
{
    internal static ImmutableArray<GeneratedSourceResult> RunInterceptorGenerator(string json, string appHostCode)
    {
        // First run the SharedResourceGenerator to produce the Add{Name} extension methods
        var resourceGenerator = new SharedResourceGenerator();
        var interceptorGenerator = new SharedResourceInterceptorGenerator();

        // Get Aspire.Hosting assembly references
        var aspireHostingAssembly = typeof(Aspire.Hosting.IDistributedApplicationBuilder).Assembly;
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(aspireHostingAssembly.Location),
        };

        // Add all referenced assemblies from Aspire.Hosting
        foreach (var referencedAssembly in aspireHostingAssembly.GetReferencedAssemblies())
        {
            try
            {
                var assembly = System.Reflection.Assembly.Load(referencedAssembly);
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
            catch
            {
                // Skip assemblies that can't be loaded
            }
        }

        // Also add System.Runtime and other framework assemblies
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var systemRuntime = Path.Combine(runtimeDir, "System.Runtime.dll");
        if (File.Exists(systemRuntime))
            references.Add(MetadataReference.CreateFromFile(systemRuntime));

        var netstandard = Path.Combine(runtimeDir, "netstandard.dll");
        if (File.Exists(netstandard))
            references.Add(MetadataReference.CreateFromFile(netstandard));

        // Parse the app host code
        var syntaxTree = CSharpSyntaxTree.ParseText(appHostCode);

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAppHost",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var additionalText = new InMemoryAdditionalText("SharedResources.g.json", json);

        // PASS 1: Run SharedResourceGenerator to produce Add{Name} extension methods.
        // This makes the generated types available in the compilation's semantic model.
        GeneratorDriver firstPass = CSharpGeneratorDriver.Create(resourceGenerator)
            .AddAdditionalTexts([additionalText]);

        firstPass = firstPass.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var compilationWithGeneratedSources,
            out _);

        // PASS 2: Run SharedResourceInterceptorGenerator on the updated compilation
        // that now includes the generated Add{Name} methods.
        GeneratorDriver secondPass = CSharpGeneratorDriver.Create(interceptorGenerator)
            .AddAdditionalTexts([additionalText]);

        secondPass = secondPass.RunGeneratorsAndUpdateCompilation(
            compilationWithGeneratedSources,
            out _,
            out _);

        var runResult = secondPass.GetRunResult();

        var interceptorResult = runResult.Results
            .FirstOrDefault(r => r.Generator.GetGeneratorType() == typeof(SharedResourceInterceptorGenerator));

        if (interceptorResult.GeneratedSources.IsDefault)
            return ImmutableArray<GeneratedSourceResult>.Empty;

        return interceptorResult.GeneratedSources
            .Select(s => new GeneratedSourceResult(s.HintName, s.SourceText))
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

    internal sealed record GeneratedSourceResult(string HintName, SourceText SourceText);
}
