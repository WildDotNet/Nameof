using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Internal.Diagnostics;
using WildDotNet.Nameof.Internal.Model;
using WildDotNet.Nameof.Internal.Resolvers;

namespace WildDotNet.Nameof.Internal.Processing;

internal readonly record struct RequestProcessingResult(
    EmissionPlan? EmissionPlan,
    ImmutableArray<Diagnostic> Diagnostics);

internal sealed class NameofRequestProcessor
{
    private readonly Compilation _compilation;
    private readonly IReadOnlyList<ITypeMemberResolver> _resolvers;
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly HashSet<string> _emittedStubs = new(StringComparer.Ordinal);

    public NameofRequestProcessor(
        Compilation compilation,
        IReadOnlyList<ITypeMemberResolver> resolvers)
    {
        _compilation = compilation;
        _resolvers = resolvers;
    }

    public RequestProcessingResult Process(ParsedNameofRequest request)
    {
        var diagnostic = Validate(request);
        if (diagnostic is not null)
        {
            return WithDiagnostic(diagnostic);
        }

        if (!TryMarkRequestAsUnique(request))
        {
            return WithDiagnostic(NameofDiagnostics.CreateDuplicateRequest(request));
        }

        var resolved = ResolveRequest(request);
        if (resolved is null)
        {
            return CreateResolutionFailure(request);
        }

        return CreateSuccessfulResult(resolved);
    }

    private Diagnostic? Validate(ParsedNameofRequest request)
    {
        if (request.Generic.IsClosedGeneric)
        {
            return NameofDiagnostics.CreateClosedGeneric(request);
        }

        if (!request.Target.IsSymbolTarget &&
            !request.Generic.IsOpenDefinition &&
            !RequestValidation.IsSupportedNonGenericFullTypeName(request.Target.FullTypeName!))
        {
            return NameofDiagnostics.CreateUnsupportedFullTypeName(
                request.AttributeLocation,
                request.Target.FullTypeName!);
        }

        return null;
    }

    private bool TryMarkRequestAsUnique(ParsedNameofRequest request)
    {
        return _seen.Add(request.Target.DeduplicationKey);
    }

    private RequestProcessingResult CreateSuccessfulResult(ResolvedTypeShape resolved)
    {
        var emissionPlan = EmissionPlanFactory.Create(resolved);

        if (emissionPlan.Stub is not null && !_emittedStubs.Add(emissionPlan.Stub.Identity))
        {
            emissionPlan = emissionPlan with { Stub = null };
        }

        return new RequestProcessingResult(emissionPlan, ImmutableArray<Diagnostic>.Empty);
    }

    private RequestProcessingResult CreateResolutionFailure(ParsedNameofRequest request)
    {
        if (request.Target.AssemblyOfType is INamedTypeSymbol assemblyOfType)
        {
            return WithDiagnostic(NameofDiagnostics.CreateResolutionFailedUsingAssemblyOf(
                request,
                assemblyOfType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }

        if (request.Target.HasAssemblyName)
        {
            return WithDiagnostic(NameofDiagnostics.CreateResolutionFailedUsingAssemblyName(request));
        }

        return new RequestProcessingResult(null, ImmutableArray<Diagnostic>.Empty);
    }

    private ResolvedTypeShape? ResolveRequest(ParsedNameofRequest request)
    {
        foreach (var resolver in _resolvers)
        {
            if (resolver.CanResolve(request, _compilation))
            {
                return resolver.Resolve(request, _compilation);
            }
        }

        return null;
    }

    private static RequestProcessingResult WithDiagnostic(Diagnostic diagnostic)
    {
        return new RequestProcessingResult(null, ImmutableArray.Create(diagnostic));
    }
}
