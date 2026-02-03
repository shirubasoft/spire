using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spire.Cli;

/// <summary>
/// Handles the execution of the resource list command.
/// </summary>
public sealed class ResourceListHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Executes the list command, writing the resources as JSON to the output.
    /// </summary>
    /// <param name="resources">The global shared resources to list.</param>
    /// <param name="output">The text writer for standard output.</param>
    /// <returns>The exit code (always 0).</returns>
    public int Execute(GlobalSharedResources resources, TextWriter output)
    {
        var json = JsonSerializer.Serialize(resources, JsonOptions);
        output.WriteLine(json);
        return 0;
    }
}