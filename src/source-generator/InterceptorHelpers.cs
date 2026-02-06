using System.Text;
using System.Text.Json;

namespace Spire.SourceGenerator;

/// <summary>
/// Shared utilities used by both <see cref="SharedResourceGenerator"/>
/// and <see cref="SharedResourceInterceptorGenerator"/>.
/// </summary>
internal static class InterceptorHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Converts a resource ID (e.g. "my-cool-service") to a PascalCase safe C# identifier (e.g. "MyCoolService").
    /// </summary>
    internal static string ToSafeIdentifier(string name)
    {
        var sb = new StringBuilder();
        var capitalizeNext = true;

        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses the SharedResources.g.json content into the configuration model.
    /// </summary>
    internal static SharedResourcesConfig ParseConfig(string json)
    {
        return JsonSerializer.Deserialize<SharedResourcesConfig>(json, JsonOptions);
    }
}

internal sealed class SharedResourcesConfig
{
    public Dictionary<string, ResourceEntry> Resources { get; set; }
}

internal sealed class ResourceEntry
{
    public string Mode { get; set; }
    public ContainerModeEntry ContainerMode { get; set; }
    public ProjectModeEntry ProjectMode { get; set; }
}

internal sealed class ContainerModeEntry
{
    public string ImageName { get; set; }
    public string ImageRegistry { get; set; }
    public string ImageTag { get; set; }
    public string BuildCommand { get; set; }
    public string BuildWorkingDirectory { get; set; }
}

internal sealed class ProjectModeEntry
{
    public string ProjectPath { get; set; }
}
