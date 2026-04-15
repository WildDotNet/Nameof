using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WildDotNet.Nameof.Internal.Generation;
using WildDotNet.Nameof.Internal.Model;
using WildDotNet.Nameof.Internal.Requests;
using WildDotNet.Nameof.Internal.Resolvers;
using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof;

[Generator]
internal sealed class NameofGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor UnsupportedFullTypeNameDescriptor = new(
        id: "NAMEOF001",
        title: "Unsupported full type name",
        messageFormat: @"GenerateNameof(""{0}"") is not supported. Only non-generic, non-nested full type names are supported (example: ""Namespace.Type"").",
        category: "NameofGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ResolutionFailedUsingAssemblyOfDescriptor = new(
        id: "NAMEOF002",
        title: "Type resolution failed",
        messageFormat: @"Could not resolve type ""{0}"" using assemblyOf ""{1}"". No members were generated.",
        category: "NameofGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ResolutionFailedUsingAssemblyNameDescriptor = new(
        id: "NAMEOF003",
        title: "Type resolution failed",
        messageFormat: @"Could not resolve type ""{0}"" using assemblyName ""{1}"". No members were generated.",
        category: "NameofGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource("WildDotNet.Nameof.Core.g.cs", SourceText.From(NameofCoreSource.Text, Encoding.UTF8));
        });

        var pipeline = context.CompilationProvider
            .Select(static (compilation, _) => WithAllMetadata(compilation))
            .Select(static (compilation, _) => (Compilation: compilation, Requests: NameofRequestCollector.Collect(compilation)));

        context.RegisterSourceOutput(pipeline, static (spc, input) =>
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            ITypeMemberResolver[] resolvers =
            [
                new CurrentCompilationMemberResolver(),
                new ExternalReflectionMemberResolver()
            ];

            foreach (var request in input.Requests)
            {
                if (request.Symbol is not null)
                {
                    var key = request.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (!seen.Add(key))
                    {
                        continue;
                    }

                    var resolved = ResolveRequest(resolvers, request, input.Compilation);
                    if (resolved is null)
                    {
                        continue;
                    }

                    AddResolvedSource(spc, resolved);

                    continue;
                }

                if (request.FullTypeName is null)
                {
                    continue;
                }

                if (!IsSupportedFullTypeName(request.FullTypeName))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        UnsupportedFullTypeNameDescriptor,
                        Location.None,
                        request.FullTypeName));

                    continue;
                }

                if (request.AssemblyOfType is not null)
                {
                    var key = $"type|{request.AssemblyOfType.ContainingAssembly.Identity.Name}|{request.FullTypeName}";
                    if (!seen.Add(key))
                    {
                        continue;
                    }

                    var resolved = ResolveRequest(resolvers, request, input.Compilation);
                    if (resolved is null)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            ResolutionFailedUsingAssemblyOfDescriptor,
                            Location.None,
                            request.FullTypeName,
                            request.AssemblyOfType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));

                        continue;
                    }

                    AddResolvedSource(spc, resolved);

                    continue;
                }

                if (request.AssemblyName is not null)
                {
                    var key = $"name|{request.AssemblyName}|{request.FullTypeName}";
                    if (!seen.Add(key))
                    {
                        continue;
                    }

                    var resolved = ResolveRequest(resolvers, request, input.Compilation);
                    if (resolved is null)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            ResolutionFailedUsingAssemblyNameDescriptor,
                            Location.None,
                            request.FullTypeName,
                            request.AssemblyName));

                        continue;
                    }

                    AddResolvedSource(spc, resolved);
                }
            }
        });
    }

    private static void AddResolvedSource(SourceProductionContext spc, ResolvedNameofType resolved)
    {
        var source = NameofSourceEmitter.EmitResolvedType(resolved);
        if (!string.IsNullOrWhiteSpace(source))
        {
            var hintIdentity = resolved.WrapperClassName.StartsWith("Nameof_", StringComparison.Ordinal)
                ? resolved.WrapperClassName["Nameof_".Length..]
                : resolved.WrapperClassName;

            spc.AddSource($"WildDotNet.Nameof.{hintIdentity}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static Compilation WithAllMetadata(Compilation compilation)
    {
        if (compilation is not CSharpCompilation csharpCompilation)
        {
            return compilation;
        }

        var options = (CSharpCompilationOptions)csharpCompilation.Options;
        if (options.MetadataImportOptions == MetadataImportOptions.All)
        {
            return compilation;
        }

        return csharpCompilation.WithOptions(options.WithMetadataImportOptions(MetadataImportOptions.All));
    }

    private static bool IsSupportedFullTypeName(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return false;
        }

        if (fullTypeName.IndexOf('`') >= 0)
        {
            return false;
        }

        if (fullTypeName.IndexOf('+') >= 0)
        {
            return false;
        }

        if (fullTypeName.IndexOf('[') >= 0 || fullTypeName.IndexOf(']') >= 0)
        {
            return false;
        }

        return true;
    }

    private static ResolvedNameofType? ResolveRequest(
        IEnumerable<ITypeMemberResolver> resolvers,
        NameofRequest request,
        Compilation compilation)
    {
        foreach (var resolver in resolvers)
        {
            if (resolver.CanResolve(request, compilation))
            {
                return resolver.Resolve(request, compilation);
            }
        }

        return null;
    }
}
