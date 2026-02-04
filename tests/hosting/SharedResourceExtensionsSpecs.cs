using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using NSubstitute;

namespace Spire.Hosting.Tests;

public class ConfigureContainerInContainerModeSpecs
{
    [Test]
    public async Task ConfigureContainer_InvokesAction()
    {
        var containerBuilder = Substitute.For<IResourceBuilder<ContainerResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)containerBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);
        var wasCalled = false;

        builder.ConfigureContainer(_ => wasCalled = true);

        await Assert.That(wasCalled).IsTrue();
    }

    [Test]
    public async Task ConfigureContainer_PassesInnerBuilderToAction()
    {
        var containerBuilder = Substitute.For<IResourceBuilder<ContainerResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)containerBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);
        IResourceBuilder<ContainerResource>? received = null;

        builder.ConfigureContainer(b => received = b);

        await Assert.That(received).IsSameReferenceAs(containerBuilder);
    }

    [Test]
    public async Task ConfigureContainer_ReturnsSameBuilder()
    {
        var containerBuilder = Substitute.For<IResourceBuilder<ContainerResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)containerBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);

        var result = builder.ConfigureContainer(_ => { });

        await Assert.That(result).IsSameReferenceAs(builder);
    }
}

public class ConfigureContainerInProjectModeSpecs
{
    [Test]
    public async Task ConfigureContainer_DoesNotInvokeAction()
    {
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        var inner = Substitute.For<IResource>();
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Project, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);
        var wasCalled = false;

        builder.ConfigureContainer(_ => wasCalled = true);

        await Assert.That(wasCalled).IsFalse();
    }

    [Test]
    public async Task ConfigureContainer_ReturnsSameBuilder()
    {
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        var inner = Substitute.For<IResource>();
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Project, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);

        var result = builder.ConfigureContainer(_ => { });

        await Assert.That(result).IsSameReferenceAs(builder);
    }
}

public class ConfigureProjectInProjectModeSpecs
{
    [Test]
    public async Task ConfigureProject_InvokesAction()
    {
        var projectBuilder = Substitute.For<IResourceBuilder<ProjectResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)projectBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Project, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);
        var wasCalled = false;

        builder.ConfigureProject(_ => wasCalled = true);

        await Assert.That(wasCalled).IsTrue();
    }

    [Test]
    public async Task ConfigureProject_PassesInnerBuilderToAction()
    {
        var projectBuilder = Substitute.For<IResourceBuilder<ProjectResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)projectBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Project, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);
        IResourceBuilder<ProjectResource>? received = null;

        builder.ConfigureProject(b => received = b);

        await Assert.That(received).IsSameReferenceAs(projectBuilder);
    }

    [Test]
    public async Task ConfigureProject_ReturnsSameBuilder()
    {
        var projectBuilder = Substitute.For<IResourceBuilder<ProjectResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)projectBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Project, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);

        var result = builder.ConfigureProject(_ => { });

        await Assert.That(result).IsSameReferenceAs(builder);
    }
}

public class ConfigureProjectInContainerModeSpecs
{
    [Test]
    public async Task ConfigureProject_DoesNotInvokeAction()
    {
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        var inner = Substitute.For<IResource>();
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);
        var wasCalled = false;

        builder.ConfigureProject(_ => wasCalled = true);

        await Assert.That(wasCalled).IsFalse();
    }

    [Test]
    public async Task ConfigureProject_ReturnsSameBuilder()
    {
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        var inner = Substitute.For<IResource>();
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);

        var result = builder.ConfigureProject(_ => { });

        await Assert.That(result).IsSameReferenceAs(builder);
    }
}

public class ExtensionMethodsPreserveDerivedTypeSpecs
{
    [Test]
    public async Task ConfigureContainer_PreservesDerivedResourceType()
    {
        var containerBuilder = Substitute.For<IResourceBuilder<ContainerResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)containerBuilder;
        var resource = new TestResource(inner, ResourceMode.Container, innerBuilder);
        var builder = new SharedResourceBuilder<TestResource>(innerBuilder, resource);

        IResourceBuilder<TestResource> result = builder.ConfigureContainer(_ => { });

        await Assert.That(result.Resource).IsTypeOf<TestResource>();
    }

    [Test]
    public async Task ConfigureProject_PreservesDerivedResourceType()
    {
        var projectBuilder = Substitute.For<IResourceBuilder<ProjectResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)projectBuilder;
        var resource = new TestResource(inner, ResourceMode.Project, innerBuilder);
        var builder = new SharedResourceBuilder<TestResource>(innerBuilder, resource);

        IResourceBuilder<TestResource> result = builder.ConfigureProject(_ => { });

        await Assert.That(result.Resource).IsTypeOf<TestResource>();
    }

    private sealed class TestResource : Aspire.Hosting.SharedResource
    {
        public TestResource(IResource inner, ResourceMode mode, IResourceBuilder<IResource> innerBuilder)
            : base(inner, mode, innerBuilder) { }
    }
}
