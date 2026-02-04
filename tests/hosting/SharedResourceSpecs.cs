using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using NSubstitute;

namespace Spire.Hosting.Tests;

public class SharedResourceSpecs
{
    [Test]
    public async Task Name_DelegatesToInnerResource()
    {
        var inner = Substitute.For<IResource>();
        inner.Name.Returns("my-service");
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();

        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);

        await Assert.That(resource.Name).IsEqualTo("my-service");
    }

    [Test]
    public async Task Annotations_DelegatesToInnerResource()
    {
        var inner = Substitute.For<IResource>();
        var annotations = new ResourceAnnotationCollection();
        inner.Annotations.Returns(annotations);
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();

        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Project, innerBuilder);

        await Assert.That(resource.Annotations).IsSameReferenceAs(annotations);
    }

    [Test]
    public async Task Mode_ReturnsProvidedMode()
    {
        var inner = Substitute.For<IResource>();
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();

        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Project, innerBuilder);

        await Assert.That(resource.Mode).IsEqualTo(ResourceMode.Project);
    }

    [Test]
    public async Task InnerBuilder_ReturnsProvidedBuilder()
    {
        var inner = Substitute.For<IResource>();
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();

        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);

        await Assert.That(resource.InnerBuilder).IsSameReferenceAs(innerBuilder);
    }

    [Test]
    public async Task CanBeInherited()
    {
        var inner = Substitute.For<IResource>();
        inner.Name.Returns("derived");
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();

        var derived = new TestResource(inner, ResourceMode.Container, innerBuilder);

        await Assert.That(derived.Name).IsEqualTo("derived");
        await Assert.That(derived.Mode).IsEqualTo(ResourceMode.Container);
    }

    private sealed class TestResource : Aspire.Hosting.SharedResource
    {
        public TestResource(IResource inner, ResourceMode mode, IResourceBuilder<IResource> innerBuilder)
            : base(inner, mode, innerBuilder) { }
    }
}
