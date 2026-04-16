using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Internal.Model;
using WildDotNet.Nameof.Internal.Policies;
using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof.Internal.Resolvers;

internal sealed class ExternalReflectionMemberResolver : ITypeMemberResolver
{
    public bool CanResolve(ParsedNameofRequest request, Compilation compilation)
    {
        if (request.Target.Symbol is INamedTypeSymbol symbol)
        {
            return !SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, compilation.Assembly);
        }

        return !string.Equals(request.Target.RequestedAssemblyName, compilation.Assembly.Identity.Name, StringComparison.Ordinal);
    }

    public ResolvedTypeShape? Resolve(ParsedNameofRequest request, Compilation compilation)
    {
        if (request.Target.Symbol is INamedTypeSymbol symbolTarget)
        {
            return ResolveFromSymbolTarget(compilation, request, symbolTarget);
        }

        if (request.Target.FullTypeName is not string fullTypeName)
        {
            return null;
        }

        if (request.Target.AssemblyOfType is INamedTypeSymbol assemblyOfType)
        {
            return ResolveFromAssemblyOfTarget(compilation, request, fullTypeName, assemblyOfType);
        }

        return ResolveFromAssemblyNameTarget(compilation, request, fullTypeName);
    }

    private static ResolvedTypeShape? ResolveFromSymbolTarget(
        Compilation compilation,
        ParsedNameofRequest request,
        INamedTypeSymbol symbolTarget)
    {
        var metadataFullTypeName = TypeNameUtilities.GetMetadataFullName(symbolTarget);
        var assemblyName = symbolTarget.ContainingAssembly.Identity.Name;
        var runtimeType = LoadRuntimeType(compilation, metadataFullTypeName, assemblyName);
        if (runtimeType is null)
        {
            return null;
        }

        runtimeType = NormalizeOpenGeneric(runtimeType, request);
        return ResolvedTypeShapeFactory.CreateResolvedSymbolType(
            symbolTarget,
            MemberInclusionPolicy.FilterReflectedMembers(
                symbolTarget,
                MemberInclusionPolicy.ExtractReflectedMembers(runtimeType, includePublicMembers: true, declaredOnly: true),
                compilation),
            request.Generic.IsOpenDefinition);
    }

    private static ResolvedTypeShape? ResolveFromAssemblyOfTarget(
        Compilation compilation,
        ParsedNameofRequest request,
        string fullTypeName,
        INamedTypeSymbol assemblyOfType)
    {
        var assemblyName = assemblyOfType.ContainingAssembly.Identity.Name;
        var assemblyOfFullName = TypeNameUtilities.GetMetadataFullName(assemblyOfType);
        var targetAssembly = LoadAssemblyFromAnchor(assemblyName, assemblyOfFullName);
        var runtimeType = targetAssembly?.GetType(fullTypeName, throwOnError: false);
        if (runtimeType is null)
        {
            return null;
        }

        runtimeType = NormalizeOpenGeneric(runtimeType, request);
        var resolvedSymbol = ReferencedTypeSymbolLocator.FindTypeSymbol(assemblyOfType.ContainingAssembly, fullTypeName);
        return CreateResolvedShape(compilation, request, runtimeType, resolvedSymbol);
    }

    private static ResolvedTypeShape? ResolveFromAssemblyNameTarget(
        Compilation compilation,
        ParsedNameofRequest request,
        string fullTypeName)
    {
        var runtimeType = LoadRuntimeType(compilation, fullTypeName, request.Target.RequestedAssemblyName);
        if (runtimeType is null)
        {
            return null;
        }

        runtimeType = NormalizeOpenGeneric(runtimeType, request);
        var referencedSymbol = ReferencedTypeSymbolLocator.FindReferencedTypeSymbol(
            compilation,
            request.Target.RequestedAssemblyName,
            fullTypeName);
        return CreateResolvedShape(compilation, request, runtimeType, referencedSymbol);
    }

    private static ResolvedTypeShape? CreateResolvedShape(
        Compilation compilation,
        ParsedNameofRequest request,
        Type runtimeType,
        INamedTypeSymbol? referencedSymbol)
    {
        return referencedSymbol is not null
            ? ResolvedTypeShapeFactory.CreateResolvedSymbolType(
                referencedSymbol,
                MemberInclusionPolicy.FilterReflectedMembers(
                    referencedSymbol,
                    MemberInclusionPolicy.ExtractReflectedMembers(runtimeType, includePublicMembers: true, declaredOnly: true),
                    compilation),
                request.Generic.IsOpenDefinition)
            : ResolvedTypeShapeFactory.CreateResolvedRuntimeType(
                compilation,
                runtimeType,
                MemberInclusionPolicy.FilterReflectedMembers(
                    null,
                    MemberInclusionPolicy.ExtractReflectedMembers(runtimeType, includePublicMembers: true, declaredOnly: false),
                    compilation),
                request.Generic.IsOpenDefinition);
    }

    private static Type? LoadRuntimeType(Compilation compilation, string fullTypeName, string assemblyName)
    {
        return TryFindLoadedType(fullTypeName) ??
               TryLoadTypeFromReferences(compilation, fullTypeName) ??
               Type.GetType($"{fullTypeName}, {assemblyName}", throwOnError: false) ??
               TryLoadAssemblyFromReferences(compilation, assemblyName)?.GetType(fullTypeName, throwOnError: false) ??
               TryLoadAssemblyByName(assemblyName)?.GetType(fullTypeName, throwOnError: false);
    }

    private static Assembly? LoadAssemblyFromAnchor(string assemblyName, string assemblyOfFullName)
    {
        var whereRuntimeType = TryFindLoadedType(assemblyOfFullName) ??
            Type.GetType($"{assemblyOfFullName}, {assemblyName}", throwOnError: false) ??
            TryLoadAssemblyByName(assemblyName)?.GetType(assemblyOfFullName, throwOnError: false);

        return whereRuntimeType?.Assembly ?? TryLoadAssemblyByName(assemblyName);
    }

    private static Type NormalizeOpenGeneric(Type runtimeType, ParsedNameofRequest request)
    {
        if (request.Generic.IsOpenDefinition && runtimeType.IsGenericType && !runtimeType.IsGenericTypeDefinition)
        {
            return runtimeType.GetGenericTypeDefinition();
        }

        return runtimeType;
    }

#pragma warning disable RS1035
    private static Assembly? TryLoadAssemblyByName(string assemblyName)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal));

        if (loaded is not null)
        {
            return loaded;
        }

        try
        {
            return Assembly.Load(assemblyName);
        }
        catch
        {
            return null;
        }
    }
#pragma warning restore RS1035

    private static Assembly? TryLoadAssemblyFromReferences(Compilation compilation, string assemblyName)
    {
        foreach (var reference in compilation.References)
        {
            if (reference is not PortableExecutableReference peReference)
            {
                continue;
            }

            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
            {
                continue;
            }

            if (!string.Equals(assemblySymbol.Identity.Name, assemblyName, StringComparison.Ordinal))
            {
                continue;
            }

            var path = peReference.FilePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            try
            {
                return Assembly.LoadFrom(path);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

#pragma warning disable RS1035
    private static Type? TryLoadTypeFromReferences(Compilation compilation, string fullTypeName)
    {
        foreach (var reference in compilation.References)
        {
            if (reference is not PortableExecutableReference peReference)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(peReference.FilePath))
            {
                continue;
            }

            try
            {
                var assembly = Assembly.LoadFrom(peReference.FilePath);
                var type = assembly.GetType(fullTypeName, throwOnError: false);
                if (type is not null)
                {
                    return type;
                }
            }
            catch
            {
            }
        }

        return null;
    }
#pragma warning restore RS1035

    private static Type? TryFindLoadedType(string fullTypeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullTypeName, throwOnError: false);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }
}
