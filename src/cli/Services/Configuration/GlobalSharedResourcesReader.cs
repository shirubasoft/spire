using Microsoft.Extensions.Configuration;

using Spire.Cli.Services.Git;

namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Reads shared resources from the global config file and resolves each resource's
/// image tag to the current Git branch tag.
/// </summary>
public sealed class GlobalSharedResourcesReader : IGlobalSharedResourcesReader
{
    private const string ConfigFileName = "aspire-shared-resources.json";

    private readonly IGitService _gitService;
    private readonly IImageTagGenerator _tagGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalSharedResourcesReader"/> class.
    /// </summary>
    /// <param name="gitService">The Git service for repository operations.</param>
    /// <param name="tagGenerator">The image tag generator.</param>
    public GlobalSharedResourcesReader(IGitService gitService, IImageTagGenerator tagGenerator)
    {
        _gitService = gitService;
        _tagGenerator = tagGenerator;
    }

    /// <inheritdoc />
    public async Task<GlobalSharedResources> GetSharedResourcesAsync(CancellationToken cancellationToken = default)
    {
        var resources = LoadFromDisk();
        return await ResolveImageTagsAsync(resources, cancellationToken);
    }

    private static GlobalSharedResources LoadFromDisk()
    {
        var globalConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "spire",
            ConfigFileName);

        var builder = new ConfigurationBuilder()
            .AddJsonFile(globalConfigPath, optional: true, reloadOnChange: false);

        var configuration = builder.Build();

        var result = configuration.Get<GlobalSharedResources>();
        return result?.Resources is null ? GlobalSharedResources.Empty : result;
    }

    private async Task<GlobalSharedResources> ResolveImageTagsAsync(
        GlobalSharedResources resources,
        CancellationToken cancellationToken)
    {
        var updatedResources = new Dictionary<string, SharedResource>(resources.Resources);

        foreach (var (id, resource) in resources.Resources)
        {
            if (resource.ContainerMode is null)
            {
                continue;
            }

            try
            {
                var repository = await _gitService.GetRepositoryAsync(
                    resource.ContainerMode.BuildWorkingDirectory, cancellationToken);

                var tags = _tagGenerator.Generate(repository);

                updatedResources[id] = resource with
                {
                    ContainerMode = resource.ContainerMode with
                    {
                        ImageTag = tags.BranchTag
                    }
                };
            }
            catch
            {
                // If git resolution fails (repo not cloned, git unavailable, etc.),
                // keep the original ImageTag from the config file.
            }
        }

        return resources with { Resources = updatedResources };
    }
}
