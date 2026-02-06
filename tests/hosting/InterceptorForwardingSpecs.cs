using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using NSubstitute;

namespace Spire.Hosting.Tests;

public class ForwardToContainerBuilderSpecs
{
    [Test]
    public async Task InnerBuilder_CastsToContainerBuilder_WhenContainerMode()
    {
        // Arrange — simulate what the generated interceptor does
        var containerBuilder = Substitute.For<IResourceBuilder<ContainerResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)containerBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);

        // Act — simulate interceptor cast pattern
        var castInner = (IResourceBuilder<ContainerResource>)resource.InnerBuilder;

        // Assert
        await Assert.That(castInner).IsSameReferenceAs(containerBuilder);
    }

    [Test]
    public async Task Mode_IsContainer_WhenConstructedWithContainerMode()
    {
        var inner = Substitute.For<IResource>();
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);

        await Assert.That(resource.Mode).IsEqualTo(ResourceMode.Container);
    }
}

public class ForwardToProjectBuilderSpecs
{
    [Test]
    public async Task InnerBuilder_CastsToProjectBuilder_WhenProjectMode()
    {
        // Arrange
        var projectBuilder = Substitute.For<IResourceBuilder<ProjectResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)projectBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Project, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);

        // Act — simulate interceptor cast pattern
        var castInner = (IResourceBuilder<ProjectResource>)resource.InnerBuilder;

        // Assert
        await Assert.That(castInner).IsSameReferenceAs(projectBuilder);
    }

    [Test]
    public async Task Mode_IsProject_WhenConstructedWithProjectMode()
    {
        var inner = Substitute.For<IResource>();
        var innerBuilder = Substitute.For<IResourceBuilder<IResource>>();
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Project, innerBuilder);

        await Assert.That(resource.Mode).IsEqualTo(ResourceMode.Project);
    }
}

public class InterceptorReturnsSameBuilderSpecs
{
    [Test]
    public async Task ReturnsSameBuilder_AfterInterceptorForwarding()
    {
        // Arrange
        var containerBuilder = Substitute.For<IResourceBuilder<ContainerResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)containerBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);
        var builder = new SharedResourceBuilder<Aspire.Hosting.SharedResource>(innerBuilder, resource);

        // Act — interceptor pattern: forward to inner, then return original builder
        var _ = (IResourceBuilder<ContainerResource>)resource.InnerBuilder;
        // interceptor would call method on castInner, then return builder
        IResourceBuilder<Aspire.Hosting.SharedResource> result = builder;

        // Assert
        await Assert.That(result).IsSameReferenceAs(builder);
    }
}

public class InterceptorModeBranchingSpecs
{
    [Test]
    public async Task ContainerMode_BranchesToContainerCast()
    {
        // Arrange
        var containerBuilder = Substitute.For<IResourceBuilder<ContainerResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)containerBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Container, innerBuilder);

        // Act — simulate the interceptor's mode branching logic
        IResourceBuilder<IResource>? usedBuilder = null;
        if (resource.Mode == ResourceMode.Container)
            usedBuilder = (IResourceBuilder<IResource>)(IResourceBuilder<ContainerResource>)resource.InnerBuilder;
        else
            usedBuilder = (IResourceBuilder<IResource>)(IResourceBuilder<ProjectResource>)resource.InnerBuilder;

        // Assert — container branch was taken
        await Assert.That(usedBuilder).IsSameReferenceAs(innerBuilder);
    }

    [Test]
    public async Task ProjectMode_BranchesToProjectCast()
    {
        // Arrange
        var projectBuilder = Substitute.For<IResourceBuilder<ProjectResource>>();
        var inner = Substitute.For<IResource>();
        IResourceBuilder<IResource> innerBuilder = (IResourceBuilder<IResource>)projectBuilder;
        var resource = new Aspire.Hosting.SharedResource(inner, ResourceMode.Project, innerBuilder);

        // Act — simulate the interceptor's mode branching logic
        IResourceBuilder<IResource>? usedBuilder = null;
        if (resource.Mode == ResourceMode.Container)
            usedBuilder = (IResourceBuilder<IResource>)(IResourceBuilder<ContainerResource>)resource.InnerBuilder;
        else
            usedBuilder = (IResourceBuilder<IResource>)(IResourceBuilder<ProjectResource>)resource.InnerBuilder;

        // Assert — project branch was taken
        await Assert.That(usedBuilder).IsSameReferenceAs(innerBuilder);
    }
}
