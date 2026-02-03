namespace Spire.Tests;

/// <summary>
/// Tests for resource lookup operations when the resource exists.
/// </summary>
public class WhenResourceExistsSpecs
{
    private readonly GlobalSharedResources _resources;
    private readonly SharedResource _expectedResource;

    public WhenResourceExistsSpecs()
    {
        _expectedResource = new SharedResource
        {
            Mode = Mode.Container,
            ContainerMode = new ContainerModeSettings
            {
                ImageName = "postgres",
                ImageRegistry = "docker.io",
                ImageTag = "latest",
                BuildCommand = "docker build -t postgres .",
                BuildWorkingDirectory = "/home/user/projects/postgres"
            },
            ProjectMode = null,
            GitRepository = new GitRepositorySettings
            {
                Url = new Uri("https://github.com/org/repo"),
                DefaultBranch = "main"
            }
        };

        _resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = _expectedResource
            }
        };
    }

    [Test]
    public async Task TryGetResource_WhenExists_ReturnsTrue()
    {
        var result = _resources.TryGetResource("postgres", out var resource);

        await Assert.That(result).IsTrue();
        await Assert.That(resource).IsNotNull();
        await Assert.That(resource).IsEqualTo(_expectedResource);
    }

    [Test]
    public async Task GetResource_WhenExists_ReturnsResource()
    {
        var resource = _resources.GetResource("postgres");

        await Assert.That(resource).IsNotNull();
        await Assert.That(resource).IsEqualTo(_expectedResource);
    }
}

/// <summary>
/// Tests for resource lookup operations when the resource does not exist.
/// </summary>
public class WhenResourceNotExistsSpecs
{
    private readonly GlobalSharedResources _resources;

    public WhenResourceNotExistsSpecs()
    {
        _resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>()
        };
    }

    [Test]
    public async Task TryGetResource_WhenNotExists_ReturnsFalse()
    {
        var result = _resources.TryGetResource("unknown", out var resource);

        await Assert.That(result).IsFalse();
        await Assert.That(resource).IsNull();
    }

    [Test]
    public async Task GetResource_WhenNotExists_ReturnsNull()
    {
        var resource = _resources.GetResource("unknown");

        await Assert.That(resource).IsNull();
    }
}

/// <summary>
/// Tests for empty GlobalSharedResources.
/// </summary>
public class EmptyGlobalSharedResourcesSpecs
{
    [Test]
    public async Task Empty_ReturnsEmptyResources()
    {
        await Assert.That(GlobalSharedResources.Empty.Resources).IsEmpty();
    }

    [Test]
    public async Task TryGetResource_OnEmpty_ReturnsFalse()
    {
        var result = GlobalSharedResources.Empty.TryGetResource("any", out var resource);

        await Assert.That(result).IsFalse();
        await Assert.That(resource).IsNull();
    }

    [Test]
    public async Task GetResource_OnEmpty_ReturnsNull()
    {
        var resource = GlobalSharedResources.Empty.GetResource("any");

        await Assert.That(resource).IsNull();
    }
}