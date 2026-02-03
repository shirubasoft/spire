# System.CommandLine 2.0.2 Reference

## Command Creation

```csharp
// Commands require mandatory name parameter
RootCommand root = new("App description");
Command sub = new("subcommand", "Description");

// Add subcommands and options via collections
root.Subcommands.Add(sub);
root.Options.Add(myOption);
root.Arguments.Add(myArgument);

// Parsing and invocation
return root.Parse(args).Invoke();           // Synchronous
return await root.Parse(args).InvokeAsync(); // Asynchronous
```

## Options

```csharp
// Options with aliases (long form, short form)
Option<string> nameOption = new("--name", "-n")
{
    Description = "Your name"
};

// Required option
Option<string> fileOption = new("--file", "-f")
{
    Description = "Input file path",
    Required = true
};

// Default value via factory
Option<int> portOption = new("--port")
{
    DefaultValueFactory = _ => 8080
};

// Restricted values
Option<string> formatOption = new("--format");
formatOption.AcceptOnlyFromAmong("json", "xml", "yaml");

// File validation (must exist)
Option<FileInfo> configOption = new("--config");
configOption.AcceptExistingOnly();

// Multiple values
Option<string[]> tagsOption = new("--tags")
{
    Arity = ArgumentArity.OneOrMore
};

// Custom validation
Option<int> delayOption = new("--delay", "-d")
{
    DefaultValueFactory = _ => 1000
};
delayOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() < 0)
        result.AddError("Delay must be non-negative");
});
```

## Arguments

```csharp
// Positional argument (no -- prefix)
Argument<FileInfo> fileArgument = new("file")
{
    Description = "The file to process"
};

// Add to command
command.Arguments.Add(fileArgument);
```

## Setting Actions (Handlers)

```csharp
// Synchronous - optionally returns int exit code
command.SetAction(parseResult =>
{
    string value = parseResult.GetValue(myOption);
    DoWork(value);
    return 0; // exit code (optional)
});

// Asynchronous - CancellationToken for Ctrl+C handling
command.SetAction(async (parseResult, cancellationToken) =>
{
    string value = parseResult.GetValue(myOption);
    await DoWorkAsync(value, cancellationToken);
    return 0;
});
```

## Getting Values

```csharp
// By option/argument reference (preferred)
int count = parseResult.GetValue(countOption);
FileInfo file = parseResult.GetValue(fileArgument);

// By name (must match exactly including --)
int count = parseResult.GetValue<int>("--count");
string path = parseResult.GetValue<string>("file");

// Required value (throws if missing/invalid)
int count = parseResult.GetRequiredValue(countOption);
int count = parseResult.GetRequiredValue<int>("--count");
```

## Console Output (Testable Pattern)

Extract logic into services that accept `TextWriter`:

```csharp
// Service with testable output
public class ResourceListService
{
    public int Execute(GlobalSharedResources resources, TextWriter output)
    {
        var json = JsonSerializer.Serialize(resources);
        output.WriteLine(json);
        return 0;
    }
}

// Command handler wires it up
command.SetAction(parseResult =>
{
    var resources = SharedResourcesConfigurationExtensions.GetSharedResources();
    return new ResourceListService().Execute(resources, Console.Out);
});
```

## Testing

### Unit Test (Service Layer)

```csharp
[Test]
public void Execute_WithResources_WritesJson()
{
    var output = new StringWriter();
    var resources = new GlobalSharedResources { Resources = new() };
    var service = new ResourceListService();

    int exitCode = service.Execute(resources, output);

    Assert.That(exitCode, Is.EqualTo(0));
    Assert.That(output.ToString(), Does.Contain("resources"));
}
```

### Integration Test (Full Command)

```csharp
[Test]
public async Task ListCommand_ReturnsJson()
{
    var output = new StringWriter();
    var error = new StringWriter();
    var config = new InvocationConfiguration
    {
        Output = output,
        Error = error
    };

    int exitCode = await command.Parse([]).InvokeAsync(config);

    Assert.That(exitCode, Is.EqualTo(0));
    Assert.That(output.ToString(), Does.Contain("{"));
}
```

### InvocationConfiguration Properties

| Property | Description | Default |
| -------- | ----------- | ------- |
| `Output` | Standard output stream | `Console.Out` |
| `Error` | Standard error stream | `Console.Error` |
| `EnableDefaultExceptionHandler` | Catch unhandled exceptions | `true` |
| `ProcessTerminationTimeout` | Ctrl+C graceful shutdown timeout | 2 seconds |

## Exit Codes

- Return `int` from action for exit code
- Default is `0` if not specified
- Use non-zero for errors
- `130` is conventional for user cancellation (Ctrl+C)

```csharp
command.SetAction(parseResult =>
{
    if (!Validate())
    {
        Console.Error.WriteLine("Validation failed");
        return 1;
    }
    return 0;
});
```

## Key API Summary

| Task | API |
| ---- | --- |
| Add option to command | `command.Options.Add(opt)` |
| Add argument to command | `command.Arguments.Add(arg)` |
| Add subcommand | `command.Subcommands.Add(cmd)` |
| Set handler | `command.SetAction(parseResult => ...)` |
| Set async handler | `command.SetAction(async (parseResult, ct) => ...)` |
| Parse args | `command.Parse(args)` |
| Invoke sync | `parseResult.Invoke()` |
| Invoke async | `parseResult.InvokeAsync()` |
| Get value by ref | `parseResult.GetValue(option)` |
| Get value by name | `parseResult.GetValue<T>("--name")` |
| Require option | `option.Required = true` |
| Restrict values | `option.AcceptOnlyFromAmong(...)` |
| Validate file exists | `option.AcceptExistingOnly()` |
| Custom validation | `option.Validators.Add(...)` |

## Spectre.Console Integration

For interactive prompts and rich console output, use `IAnsiConsole` from Spectre.Console.

### Handler with IAnsiConsole

```csharp
public class ResourceRemoveHandler
{
    private readonly IAnsiConsole _console;
    private readonly ISharedResourcesWriter _writer;

    public ResourceRemoveHandler(IAnsiConsole console, ISharedResourcesWriter writer)
    {
        _console = console;
        _writer = writer;
    }

    public int Execute(string id, bool yes)
    {
        // Confirmation prompt (skipped if --yes)
        if (!yes && !_console.Confirm($"Remove resource '{id}'?", defaultValue: false))
            return 0;

        // Perform operation
        _writer.RemoveResource(id);

        // Success output with markup
        _console.MarkupLine($"[green]Removed '{id}'[/]");
        return 0;
    }
}
```

### Interactive Selection

```csharp
var choice = _console.Prompt(
    new SelectionPrompt<string>()
        .Title("Select a resource:")
        .PageSize(10)
        .AddChoices(resources.Keys));
```

### User Input

```csharp
var id = _console.Ask<string>("Enter resource ID:");
var port = _console.Ask<int>("Enter port number:", 8080); // with default
```

## Testing with TestConsole

Use `TestConsole` from `Spectre.Console.Testing` NuGet package.

### Basic Test Setup

```csharp
[Test]
public void Execute_WhenUserConfirms_RemovesResource()
{
    // Arrange
    var console = new TestConsole();
    console.Interactive();
    console.Input.PushTextWithEnter("y"); // Simulate "yes" to confirm prompt

    var writer = new MockSharedResourcesWriter();
    var handler = new ResourceRemoveHandler(console, writer);

    // Act
    var result = handler.Execute("postgres", yes: false);

    // Assert
    Assert.That(result, Is.EqualTo(0));
    Assert.That(console.Output, Does.Contain("Removed"));
    Assert.That(writer.RemovedIds, Does.Contain("postgres"));
}
```

### Testing Selection Prompts

```csharp
[Test]
public void Execute_SelectsSecondOption()
{
    var console = new TestConsole();
    console.Interactive();
    console.Input.PushKey(ConsoleKey.DownArrow); // Move to second item
    console.Input.PushKey(ConsoleKey.Enter);      // Select

    // ... test logic
}
```

### Testing Text Input

```csharp
[Test]
public void Execute_AcceptsUserInput()
{
    var console = new TestConsole();
    console.Interactive();
    console.Input.PushTextWithEnter("my-resource"); // Simulate typing + Enter

    // ... test logic
}
```

### Key TestConsole Methods

| Method | Purpose |
| ------ | ------- |
| `console.Interactive()` | Enable interactive mode (required for prompts) |
| `console.Input.PushTextWithEnter("text")` | Simulate typing text and pressing Enter |
| `console.Input.PushKey(ConsoleKey.Enter)` | Simulate pressing a key |
| `console.Output` | Get all console output as string |
| `console.Lines` | Get output split by lines |
