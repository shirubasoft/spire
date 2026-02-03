using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spire.Cli;

/// <summary>
/// Handles the execution of the resource info command.
/// </summary>
public sealed class ResourceInfoHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Executes the info command, writing the resource as JSON to the output.
    /// </summary>
    /// <param name="id">The resource ID to look up.</param>
    /// <param name="resources">The global shared resources.</param>
    /// <param name="output">The text writer for standard output.</param>
    /// <param name="error">The text writer for standard error.</param>
    /// <returns>0 if the resource was found; 1 otherwise.</returns>
    public int Execute(string id, GlobalSharedResources resources, TextWriter output, TextWriter error)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            error.WriteLine("Error: Resource ID is required.");
            return 1;
        }

        if (!resources.TryGetResource(id, out var resource))
        {
            error.WriteLine($"Error: Resource '{id}' not found.");
            return 1;
        }

        var json = JsonSerializer.Serialize(resource, JsonOptions);
        output.WriteLine(json);
        return 0;
    }
}