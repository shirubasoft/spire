using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using NSubstitute;

namespace Spire.Hosting.Tests;

public class SharedResourceBuilderSpecs
{
    [Test]
    public async Task Resource_ReturnsProvidedResource()
    {
        var inner = Substitute.For<IResource>();
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);

        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);

        await Assert.That(builder.Resource).IsSameReferenceAs(resource);
    }

    [Test]
    public async Task ApplicationBuilder_DelegatesToInnerBuilder()
    {
        var appBuilder = Substitute.For<IDistributedApplicationBuilder>();
        var inner = Substitute.For<IResource>();
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        innerBuilder.ApplicationBuilder.Returns(appBuilder);
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);

        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);

        await Assert.That(builder.ApplicationBuilder).IsSameReferenceAs(appBuilder);
    }

    [Test]
    public async Task WithAnnotation_DelegatesToInnerAndReturnsSelf()
    {
        var inner = Substitute.For<IResource>();
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);

        var annotation = new TestAnnotation();
        var result = builder.WithAnnotation(annotation, ResourceAnnotationMutationBehavior.Append);

        await Assert.That(result).IsSameReferenceAs(builder);
        innerBuilder.Received(1).WithAnnotation(annotation, ResourceAnnotationMutationBehavior.Append);
    }

    [Test]
    public async Task PreservesDerivedResourceType()
    {
        var inner = Substitute.For<IResource>();
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        var resource = new DerivedResource(inner, ResourceMode.Project, innerBuilder);

        var builder = new SharedResourceBuilder<DerivedResource>(innerBuilder, resource);

        IResourceBuilder<DerivedResource> typed = builder;
        await Assert.That(typed.Resource).IsTypeOf<DerivedResource>();
    }

    [Test]
    public async Task WithAnnotation_PreservesDerivedType()
    {
        var inner = Substitute.For<IResource>();
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        var resource = new DerivedResource(inner, ResourceMode.Container, innerBuilder);
        var builder = new SharedResourceBuilder<DerivedResource>(innerBuilder, resource);

        IResourceBuilder<DerivedResource> result = builder.WithAnnotation(
            new TestAnnotation(), ResourceAnnotationMutationBehavior.Append);

        await Assert.That(result.Resource).IsTypeOf<DerivedResource>();
    }

    private sealed class DerivedResource : Aspire.Hosting.SharedResource
    {
        public DerivedResource(IResource inner, ResourceMode mode, IResourceBuilder<IResource> innerBuilder)
            : base(inner, mode, innerBuilder) { }
    }

    private sealed class TestAnnotation : IResourceAnnotation;
}
