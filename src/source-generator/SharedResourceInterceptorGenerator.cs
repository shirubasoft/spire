using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Spire.SourceGenerator;

/// <summary>
/// Generates interceptor methods that forward Aspire extension method calls
/// from <c>SharedResource</c> builders to their inner resource builders.
/// </summary>
[Generator]
public sealed class SharedResourceInterceptorGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Method names that should not be intercepted (our own extensions).
    /// </summary>
    private static readonly HashSet<string> ExcludedMethods = new()
    {
        "ConfigureContainer",
        "ConfigureProject",
    };

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. JSON Provider — extract known Add{Name} method names from SharedResources.g.json
        var addMethodNames = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith("SharedResources.g.json"))
            .Select(static (file, ct) => file.GetText(ct)?.ToString())
            .Where(static text => !string.IsNullOrWhiteSpace(text))
            .Select(static (json, _) =>
            {
                var config = InterceptorHelpers.ParseConfig(json);
                if (config?.Resources is null)
                    return ImmutableHashSet<string>.Empty;

                var builder = ImmutableHashSet.CreateBuilder<string>();
                foreach (var kvp in config.Resources)
                {
                    if (!string.IsNullOrEmpty(kvp.Key))
                    {
                        var safeName = InterceptorHelpers.ToSafeIdentifier(kvp.Key);
                        builder.Add($"Add{safeName}");
                    }
                }

                return builder.ToImmutable();
            })
            .Collect()
            .Select(static (sets, _) =>
            {
                var builder = ImmutableHashSet.CreateBuilder<string>();
                foreach (var set in sets)
                    builder.UnionWith(set);
                return builder.ToImmutable();
            });

        // 2. Syntax Provider — find invocation expressions that are fluent calls on member access
        var candidateInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateInvocation(node),
                transform: static (ctx, _) => ctx.Node)
            .Where(static node => node is not null);

        // 3. Combine candidates with known Add method names and compilation
        var combined = candidateInvocations
            .Combine(addMethodNames)
            .Combine(context.CompilationProvider);

        // 4. Filter and extract interceptor data
        var interceptorData = combined
            .Select(static (pair, ct) =>
            {
                var ((node, addNames), compilation) = pair;

                if (addNames.IsEmpty || node is not InvocationExpressionSyntax invocation)
                    return default;

                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                    return default;

                var methodName = memberAccess.Name.Identifier.Text;

                // Skip our own extension methods
                if (ExcludedMethods.Contains(methodName))
                    return default;

                // Walk the receiver chain to check if it originates from an Add{KnownResource}() call
                if (!IsSharedResourceChain(memberAccess.Expression, addNames))
                    return default;

                // Get the semantic model and interceptable location
                var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);

#pragma warning disable RSEXPERIMENTAL002
                var location = semanticModel.GetInterceptableLocation(invocation, ct);
#pragma warning restore RSEXPERIMENTAL002

                if (location is null)
                    return default;

                // Try to resolve the method symbol from the semantic model
                var symbolInfo = semanticModel.GetSymbolInfo(invocation, ct);
                var methodSymbol = symbolInfo.Symbol as IMethodSymbol
                    ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

                // If we can't resolve the symbol, try to find it in Aspire.Hosting
                if (methodSymbol is null)
                {
                    methodSymbol = ResolveAspireMethod(compilation, methodName, invocation);
                }

                if (methodSymbol is null)
                    return default;

                return new InterceptorCandidate(
                    methodName,
                    location.Version,
                    location.Data,
                    location.GetDisplayLocation(),
                    GetMethodSignature(methodSymbol));
            })
            .Where(static c => c is not null)
            .Collect();

        // 5. Generate interceptor source
        context.RegisterSourceOutput(interceptorData, static (ctx, candidates) =>
        {
            if (candidates.IsEmpty)
                return;

            var source = GenerateInterceptorSource(candidates);
            ctx.AddSource("SharedResourceInterceptors.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    /// <summary>
    /// Quick syntactic check: is this an invocation on a member access expression?
    /// </summary>
    private static bool IsCandidateInvocation(SyntaxNode node)
    {
        return node is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
        };
    }

    /// <summary>
    /// Walks the receiver chain to determine if the call originates from an Add{KnownResource}() call.
    /// </summary>
    private static bool IsSharedResourceChain(ExpressionSyntax receiver, ImmutableHashSet<string> addMethodNames)
    {
        // Walk the fluent call chain
        var current = receiver;
        while (current is not null)
        {
            switch (current)
            {
                // Chained call: something.Method(...)
                case InvocationExpressionSyntax invocation
                    when invocation.Expression is MemberAccessExpressionSyntax innerMemberAccess:
                {
                    var name = innerMemberAccess.Name.Identifier.Text;
                    if (addMethodNames.Contains(name))
                        return true;

                    // Continue walking up the chain
                    current = innerMemberAccess.Expression;
                    break;
                }

                // Direct call: Method(...)
                case InvocationExpressionSyntax invocation
                    when invocation.Expression is IdentifierNameSyntax identifierName:
                {
                    if (addMethodNames.Contains(identifierName.Identifier.Text))
                        return true;

                    return false;
                }

                // Variable reference: track back to its initializer
                case IdentifierNameSyntax identifier:
                    return TryResolveVariableOrigin(identifier, addMethodNames);

                default:
                    return false;
            }
        }

        return false;
    }

    /// <summary>
    /// For a variable reference, tries to find its declaration and check
    /// if its initializer originates from an Add{KnownResource}() call.
    /// </summary>
    private static bool TryResolveVariableOrigin(IdentifierNameSyntax identifier, ImmutableHashSet<string> addMethodNames)
    {
        var variableName = identifier.Identifier.Text;

        // Walk up to find the containing block/method
        var block = identifier.FirstAncestorOrSelf<BlockSyntax>();
        if (block is null)
            return false;

        // Search for local variable declarations in the same block
        foreach (var statement in block.Statements)
        {
            if (statement is not LocalDeclarationStatementSyntax localDecl)
                continue;

            foreach (var variable in localDecl.Declaration.Variables)
            {
                if (variable.Identifier.Text != variableName)
                    continue;

                if (variable.Initializer?.Value is not ExpressionSyntax initExpr)
                    continue;

                // Check if the initializer is or contains an Add{KnownResource}() call
                return IsOrContainsAddCall(initExpr, addMethodNames);
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if an expression is or contains (via fluent chain) an Add{KnownResource}() call.
    /// </summary>
    private static bool IsOrContainsAddCall(ExpressionSyntax expression, ImmutableHashSet<string> addMethodNames)
    {
        var current = expression;
        while (current is not null)
        {
            switch (current)
            {
                case InvocationExpressionSyntax invocation
                    when invocation.Expression is MemberAccessExpressionSyntax memberAccess:
                {
                    if (addMethodNames.Contains(memberAccess.Name.Identifier.Text))
                        return true;
                    current = memberAccess.Expression;
                    break;
                }

                case InvocationExpressionSyntax invocation
                    when invocation.Expression is IdentifierNameSyntax identifierName:
                {
                    return addMethodNames.Contains(identifierName.Identifier.Text);
                }

                default:
                    return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Searches Aspire.Hosting referenced assemblies for a matching extension method.
    /// </summary>
    private static IMethodSymbol ResolveAspireMethod(
        Compilation compilation,
        string methodName,
        InvocationExpressionSyntax invocation)
    {
        var argCount = invocation.ArgumentList.Arguments.Count;

        foreach (var reference in compilation.References)
        {
            var symbol = compilation.GetAssemblyOrModuleSymbol(reference);
            if (symbol is not IAssemblySymbol assembly)
                continue;

            // Only search Aspire.Hosting assemblies
            if (!assembly.Name.StartsWith("Aspire.Hosting"))
                continue;

            var match = FindExtensionMethod(assembly.GlobalNamespace, methodName, argCount);
            if (match is not null)
                return match;
        }

        return null;
    }

    /// <summary>
    /// Recursively searches a namespace for a public static extension method with the given name.
    /// </summary>
    private static IMethodSymbol FindExtensionMethod(INamespaceSymbol ns, string methodName, int argCount)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (!type.IsStatic)
                continue;

            foreach (var member in type.GetMembers(methodName))
            {
                if (member is IMethodSymbol method
                    && method.IsStatic
                    && method.IsExtensionMethod
                    // +1 for the 'this' parameter
                    && method.Parameters.Length == argCount + 1)
                {
                    // Check if the first parameter accepts IResourceBuilder<T>
                    var firstParam = method.Parameters[0];
                    if (IsResourceBuilderType(firstParam.Type))
                        return method;
                }
            }
        }

        foreach (var childNs in ns.GetNamespaceMembers())
        {
            var match = FindExtensionMethod(childNs, methodName, argCount);
            if (match is not null)
                return match;
        }

        return null;
    }

    /// <summary>
    /// Checks if a type is IResourceBuilder&lt;T&gt; or compatible.
    /// </summary>
    private static bool IsResourceBuilderType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            return named.Name == "IResourceBuilder"
                || named.OriginalDefinition.ToDisplayString().StartsWith("Aspire.Hosting.ApplicationModel.IResourceBuilder");
        }

        return false;
    }

    /// <summary>
    /// Extracts a serializable method signature from the resolved method symbol.
    /// </summary>
    private static MethodSignature GetMethodSignature(IMethodSymbol method)
    {
        var parameters = new List<ParameterInfo>();

        // Skip the first parameter (this/extension parameter)
        for (var i = 1; i < method.Parameters.Length; i++)
        {
            var param = method.Parameters[i];
            parameters.Add(new ParameterInfo(
                param.Name,
                param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                param.HasExplicitDefaultValue,
                param.HasExplicitDefaultValue ? FormatDefaultValue(param) : null,
                param.RefKind));
        }

        var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Check if the method is generic with T : IResource constraint on the first type parameter
        var hasGenericResourceConstraint = false;
        string typeParameterName = null;
        if (method.IsGenericMethod && method.TypeParameters.Length > 0)
        {
            var tp = method.TypeParameters[0];
            typeParameterName = tp.Name;
            foreach (var constraint in tp.ConstraintTypes)
            {
                if (constraint.Name == "IResource" || constraint.AllInterfaces.Any(i => i.Name == "IResource"))
                {
                    hasGenericResourceConstraint = true;
                    break;
                }
            }
        }

        return new MethodSignature(
            method.Name,
            returnType,
            parameters,
            hasGenericResourceConstraint,
            typeParameterName);
    }

    private static string FormatDefaultValue(IParameterSymbol param)
    {
        if (!param.HasExplicitDefaultValue)
            return "default";

        var value = param.ExplicitDefaultValue;
        if (value is null)
            return "null";
        if (value is bool b)
            return b ? "true" : "false";
        if (value is string s)
            return $"\"{s}\"";
        if (value is int or long or short or byte)
            return value.ToString();

        return "default";
    }

    /// <summary>
    /// Generates the complete interceptor source file.
    /// </summary>
    private static string GenerateInterceptorSource(ImmutableArray<InterceptorCandidate> candidates)
    {
        // Group candidates by method name + signature
        var groups = candidates
            .GroupBy(c => c.MethodName)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#pragma warning disable CS9137");
        sb.AppendLine("#pragma warning disable CS8019");
        sb.AppendLine();
        sb.AppendLine("using Aspire.Hosting;");
        sb.AppendLine("using Aspire.Hosting.ApplicationModel;");
        sb.AppendLine();

        // File-scoped InterceptsLocationAttribute
        sb.AppendLine("namespace System.Runtime.CompilerServices");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]");
        sb.AppendLine("    file sealed class InterceptsLocationAttribute : global::System.Attribute");
        sb.AppendLine("    {");
        sb.AppendLine("        public InterceptsLocationAttribute(int version, string data) { }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("namespace Spire.Hosting.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    file static class SharedResourceInterceptors");
        sb.AppendLine("    {");

        foreach (var group in groups)
        {
            var representative = group.First();
            var sig = representative.Signature;

            // Emit [InterceptsLocation] attributes for all call sites
            foreach (var candidate in group)
            {
                sb.AppendLine($"        [global::System.Runtime.CompilerServices.InterceptsLocation({candidate.LocationVersion}, \"{candidate.LocationData}\")] // {candidate.DisplayLocation}");
            }

            // Generate the interceptor method
            GenerateInterceptorMethod(sb, sig);
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateInterceptorMethod(StringBuilder sb, MethodSignature sig)
    {
        var typeParam = sig.HasGenericResourceConstraint ? "<T>" : "";
        var constraint = sig.HasGenericResourceConstraint
            ? $" where T : global::Aspire.Hosting.SharedResource"
            : "";

        // Build parameter list
        var paramList = new StringBuilder();
        paramList.Append("this global::Aspire.Hosting.ApplicationModel.IResourceBuilder<");
        if (sig.HasGenericResourceConstraint)
            paramList.Append("T");
        else
            paramList.Append("global::Aspire.Hosting.SharedResource");
        paramList.Append("> builder");

        foreach (var param in sig.Parameters)
        {
            paramList.Append(", ");
            if (param.RefKind == RefKind.Ref)
                paramList.Append("ref ");
            else if (param.RefKind == RefKind.Out)
                paramList.Append("out ");
            paramList.Append(param.TypeName);
            paramList.Append(' ');
            paramList.Append(param.Name);
            if (param.HasDefaultValue)
            {
                paramList.Append(" = ");
                paramList.Append(param.DefaultValue);
            }
        }

        // Build argument list for forwarding
        var argList = new StringBuilder();
        foreach (var param in sig.Parameters)
        {
            if (argList.Length > 0)
                argList.Append(", ");
            if (param.RefKind == RefKind.Ref)
                argList.Append("ref ");
            else if (param.RefKind == RefKind.Out)
                argList.Append("out ");
            argList.Append(param.Name);
        }

        // Determine return type
        var returnType = $"global::Aspire.Hosting.ApplicationModel.IResourceBuilder<{(sig.HasGenericResourceConstraint ? "T" : "global::Aspire.Hosting.SharedResource")}>";

        sb.AppendLine($"        public static {returnType} {sig.MethodName}{typeParam}({paramList}){constraint}");
        sb.AppendLine("        {");
        sb.AppendLine("            var resource = builder.Resource;");
        sb.AppendLine("            var inner = resource.InnerBuilder;");
        sb.AppendLine($"            if (resource.Mode == global::Aspire.Hosting.ResourceMode.Container)");
        sb.AppendLine($"                ((global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.ApplicationModel.ContainerResource>)inner).{sig.MethodName}({argList});");
        sb.AppendLine("            else");
        sb.AppendLine($"                ((global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.ApplicationModel.ProjectResource>)inner).{sig.MethodName}({argList});");
        sb.AppendLine("            return builder;");
        sb.AppendLine("        }");
    }
}

internal sealed class InterceptorCandidate
{
    public InterceptorCandidate(
        string methodName,
        int locationVersion,
        string locationData,
        string displayLocation,
        MethodSignature signature)
    {
        MethodName = methodName;
        LocationVersion = locationVersion;
        LocationData = locationData;
        DisplayLocation = displayLocation;
        Signature = signature;
    }

    public string MethodName { get; }
    public int LocationVersion { get; }
    public string LocationData { get; }
    public string DisplayLocation { get; }
    public MethodSignature Signature { get; }
}

internal sealed class MethodSignature
{
    public MethodSignature(
        string methodName,
        string returnType,
        List<ParameterInfo> parameters,
        bool hasGenericResourceConstraint,
        string typeParameterName)
    {
        MethodName = methodName;
        ReturnType = returnType;
        Parameters = parameters;
        HasGenericResourceConstraint = hasGenericResourceConstraint;
        TypeParameterName = typeParameterName;
    }

    public string MethodName { get; }
    public string ReturnType { get; }
    public List<ParameterInfo> Parameters { get; }
    public bool HasGenericResourceConstraint { get; }
    public string TypeParameterName { get; }
}

internal sealed class ParameterInfo
{
    public ParameterInfo(string name, string typeName, bool hasDefaultValue, string defaultValue, RefKind refKind)
    {
        Name = name;
        TypeName = typeName;
        HasDefaultValue = hasDefaultValue;
        DefaultValue = defaultValue;
        RefKind = refKind;
    }

    public string Name { get; }
    public string TypeName { get; }
    public bool HasDefaultValue { get; }
    public string DefaultValue { get; }
    public RefKind RefKind { get; }
}
