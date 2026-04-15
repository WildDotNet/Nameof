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

    private static readonly DiagnosticDescriptor ClosedGenericTypeDescriptor = new(
        id: "NAMEOF004",
        title: "Closed generic types are not supported",
        messageFormat: @"GenerateNameof(""{0}"") is not supported. Only open generic definitions are supported. No members were generated.",
        category: "NameofGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource($"{GeneratorConstants.HintPrefix}.Core.g.cs", SourceText.From(NameofCoreSource.BaseText, Encoding.UTF8));
        });

        var pipeline = context.CompilationProvider
            .Select(static (compilation, _) => WithAllMetadata(compilation))
            .Select(static (compilation, _) => (Compilation: compilation, Requests: NameofRequestCollector.Collect(compilation)));

        context.RegisterSourceOutput(pipeline, static (spc, input) =>
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var emittedStubs = new HashSet<string>(StringComparer.Ordinal);
            ITypeMemberResolver[] resolvers =
            [
                new CurrentCompilationMemberResolver(),
                new ExternalReflectionMemberResolver()
            ];

            var genericSupportSource = NameofCoreSource.CreateGenericSupport(
                input.Requests
                    .Where(static request => request.IsOpenGenericDefinition)
                    .Select(static request => request.GenericArity));

            if (!string.IsNullOrWhiteSpace(genericSupportSource))
            {
                spc.AddSource(
                    $"{GeneratorConstants.HintPrefix}.GenericSupport.g.cs",
                    SourceText.From(genericSupportSource, Encoding.UTF8));
            }

            foreach (var request in input.Requests)
            {
                if (request.IsClosedGeneric)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        ClosedGenericTypeDescriptor,
                        Location.None,
                        GetDiagnosticDisplayName(request)));
                    continue;
                }

                if (request.Symbol is not null)
                {
                    var key = request.IsOpenGenericDefinition
                        ? TypeNameUtilities.GetMetadataFullName(request.Symbol)
                        : request.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (!seen.Add(key))
                    {
                        continue;
                    }

                    var resolved = ResolveRequest(resolvers, request, input.Compilation);
                    if (resolved is null)
                    {
                        continue;
                    }

                    AddResolvedSource(spc, resolved, emittedStubs);
                    continue;
                }

                if (request.FullTypeName is null)
                {
                    continue;
                }

                if (!request.IsOpenGenericDefinition && !IsSupportedNonGenericFullTypeName(request.FullTypeName))
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

                    AddResolvedSource(spc, resolved, emittedStubs);
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

                    AddResolvedSource(spc, resolved, emittedStubs);
                }
            }
        });
    }

    private static void AddResolvedSource(
        SourceProductionContext spc,
        ResolvedNameofType resolved,
        HashSet<string> emittedStubs)
    {
        if (resolved.EmitStub &&
            resolved.StubIdentity is not null &&
            !emittedStubs.Add(resolved.StubIdentity))
        {
            resolved = resolved with { EmitStub = false, StubIdentity = null, StubKind = null };
        }

        var source = NameofSourceEmitter.EmitResolvedType(resolved);
        if (!string.IsNullOrWhiteSpace(source))
        {
            var hintIdentity = resolved.WrapperClassName.StartsWith("NameofGeneric_", StringComparison.Ordinal)
                ? $"Generic.{resolved.WrapperClassName["NameofGeneric_".Length..]}"
                : resolved.WrapperClassName.StartsWith("Nameof_", StringComparison.Ordinal)
                    ? resolved.WrapperClassName["Nameof_".Length..]
                : resolved.WrapperClassName;

            spc.AddSource($"{GeneratorConstants.HintPrefix}.{hintIdentity}.g.cs", SourceText.From(source, Encoding.UTF8));
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

    private static bool IsSupportedNonGenericFullTypeName(string fullTypeName)
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

    private static string GetDiagnosticDisplayName(NameofRequest request)
    {
        if (request.Symbol is not null)
        {
            return request.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        return request.FullTypeName ?? string.Empty;
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
