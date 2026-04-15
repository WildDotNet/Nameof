using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Internal.Generation;
using WildDotNet.Nameof.Internal.Model;
using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof.Internal.Resolvers;

internal sealed class ExternalReflectionMemberResolver : ITypeMemberResolver
{
    public bool CanResolve(NameofRequest request, Compilation compilation)
    {
        if (request.Symbol is not null)
        {
            return !SymbolEqualityComparer.Default.Equals(request.Symbol.ContainingAssembly, compilation.Assembly);
        }

        var requestedAssemblyName = request.AssemblyOfType?.ContainingAssembly.Identity.Name ?? request.AssemblyName;
        return !string.Equals(requestedAssemblyName, compilation.Assembly.Identity.Name, StringComparison.Ordinal);
    }

    public ResolvedNameofType? Resolve(NameofRequest request, Compilation compilation)
    {
        if (request.Symbol is not null)
        {
            var fullTypeName = TypeNameUtilities.GetMetadataFullName(request.Symbol);
            var assemblyName = request.Symbol.ContainingAssembly.Identity.Name;

            var runtimeType = TryFindLoadedType(fullTypeName) ??
                TryLoadTypeFromReferences(compilation, fullTypeName) ??
                Type.GetType($"{fullTypeName}, {assemblyName}", throwOnError: false) ??
                TryLoadAssemblyFromReferences(compilation, assemblyName)?.GetType(fullTypeName, throwOnError: false) ??
                TryLoadAssemblyByName(assemblyName)?.GetType(fullTypeName, throwOnError: false);

            if (runtimeType is null)
            {
                return null;
            }

            if (request.IsOpenGenericDefinition && runtimeType.IsGenericType && !runtimeType.IsGenericTypeDefinition)
            {
                runtimeType = runtimeType.GetGenericTypeDefinition();
            }

            return NameofSourceEmitter.CreateResolvedSymbolType(
                compilation,
                request.Symbol,
                FilterMemberNames(
                    request.Symbol,
                    ExtractMemberNames(runtimeType, includePublicMembers: true, declaredOnly: true),
                    compilation),
                request.IsOpenGenericDefinition);
        }

        if (request.FullTypeName is null)
        {
            return null;
        }

        if (request.AssemblyOfType is not null)
        {
            var assemblyName = request.AssemblyOfType.ContainingAssembly.Identity.Name;
            var assemblyOfFullName = TypeNameUtilities.GetMetadataFullName(request.AssemblyOfType);

            var whereRuntimeType = TryFindLoadedType(assemblyOfFullName) ??
                Type.GetType($"{assemblyOfFullName}, {assemblyName}", throwOnError: false) ??
                TryLoadAssemblyByName(assemblyName)?.GetType(assemblyOfFullName, throwOnError: false);

            var targetAssembly = whereRuntimeType?.Assembly ?? TryLoadAssemblyByName(assemblyName);
            var resolved = targetAssembly?.GetType(request.FullTypeName, throwOnError: false);
            var symbol = FindTypeSymbol(request.AssemblyOfType.ContainingAssembly, request.FullTypeName);

            if (resolved is null)
            {
                return null;
            }

            if (request.IsOpenGenericDefinition && resolved.IsGenericType && !resolved.IsGenericTypeDefinition)
            {
                resolved = resolved.GetGenericTypeDefinition();
            }

            return symbol is not null
                ? NameofSourceEmitter.CreateResolvedSymbolType(
                    compilation,
                    symbol,
                    FilterMemberNames(
                        symbol,
                        ExtractMemberNames(resolved, includePublicMembers: true, declaredOnly: true),
                        compilation),
                    request.IsOpenGenericDefinition)
                : NameofSourceEmitter.CreateResolvedRuntimeType(
                    compilation,
                    resolved,
                    FilterMemberNames(
                        null,
                        ExtractMemberNames(resolved, includePublicMembers: true, declaredOnly: false),
                        compilation),
                    request.IsOpenGenericDefinition);
        }

        if (request.AssemblyName is null)
        {
            return null;
        }

        var type = TryFindLoadedType(request.FullTypeName);
        if (type is null)
        {
            type = TryLoadTypeFromReferences(compilation, request.FullTypeName);
        }

        if (type is null)
        {
            var assembly = TryLoadAssemblyByName(request.AssemblyName) ?? TryLoadAssemblyFromReferences(compilation, request.AssemblyName);
            type = assembly?.GetType(request.FullTypeName, throwOnError: false);
        }

        var referencedSymbol = FindReferencedTypeSymbol(compilation, request.AssemblyName, request.FullTypeName);

        if (type is null)
        {
            return null;
        }

        if (request.IsOpenGenericDefinition && type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            type = type.GetGenericTypeDefinition();
        }

        return referencedSymbol is not null
            ? NameofSourceEmitter.CreateResolvedSymbolType(
                compilation,
                referencedSymbol,
                FilterMemberNames(
                    referencedSymbol,
                    ExtractMemberNames(type, includePublicMembers: true, declaredOnly: true),
                    compilation),
                request.IsOpenGenericDefinition)
            : NameofSourceEmitter.CreateResolvedRuntimeType(
                compilation,
                type,
                FilterMemberNames(
                    null,
                    ExtractMemberNames(type, includePublicMembers: true, declaredOnly: false),
                    compilation),
                request.IsOpenGenericDefinition);
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

    private static HashSet<string> ExtractMemberNames(Type type, bool includePublicMembers, bool declaredOnly)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        var visibility = includePublicMembers
            ? BindingFlags.Public | BindingFlags.NonPublic
            : BindingFlags.NonPublic;

        var flags = BindingFlags.Instance | BindingFlags.Static | visibility;
        if (declaredOnly)
        {
            flags |= BindingFlags.DeclaredOnly;
        }

        foreach (var field in type.GetFields(flags))
        {
            AddIfRelevant(field.Name, names);
        }

        foreach (var property in type.GetProperties(flags))
        {
            AddIfRelevant(property.Name, names);
        }

        foreach (var @event in type.GetEvents(flags))
        {
            AddIfRelevant(@event.Name, names);
        }

        foreach (var method in type.GetMethods(flags))
        {
            if (!method.IsSpecialName)
            {
                AddIfRelevant(method.Name, names);
            }
        }

        return names;
    }

    private static HashSet<string> FilterMemberNames(
        INamedTypeSymbol? typeSymbol,
        HashSet<string> memberNames,
        Compilation compilation)
    {
        FilterReflectionOnlyArtifacts(memberNames);

        if (typeSymbol is null)
        {
            return memberNames;
        }

        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            var enumFieldNames = new HashSet<string>(
                typeSymbol.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(static field => !field.IsImplicitlyDeclared && field.AssociatedSymbol is null)
                    .Select(static field => field.Name),
                StringComparer.Ordinal);

            memberNames.IntersectWith(enumFieldNames);
            return memberNames;
        }

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
            {
                continue;
            }

            var name = member switch
            {
                IFieldSymbol field when field.AssociatedSymbol is null => field.Name,
                IPropertySymbol property => property.Name,
                IEventSymbol @event => @event.Name,
                IMethodSymbol method when method.MethodKind == MethodKind.Ordinary => method.Name,
                _ => null
            };

            if (name is null)
            {
                continue;
            }

            if (compilation.IsSymbolAccessibleWithin(member, compilation.Assembly))
            {
                memberNames.Remove(name);
            }
        }

        return memberNames;
    }

    private static HashSet<string> FilterReflectionOnlyArtifacts(HashSet<string> memberNames)
    {
        memberNames.Remove("value__");
        memberNames.RemoveWhere(static name =>
            name.StartsWith("<", StringComparison.Ordinal) ||
            name.StartsWith("get_", StringComparison.Ordinal) ||
            name.StartsWith("set_", StringComparison.Ordinal) ||
            name.StartsWith("add_", StringComparison.Ordinal) ||
            name.StartsWith("remove_", StringComparison.Ordinal) ||
            name is ".ctor" or ".cctor");

        return memberNames;
    }

    private static void AddIfRelevant(string name, HashSet<string> names)
    {
        if (!name.StartsWith("<", StringComparison.Ordinal))
        {
            names.Add(name);
        }
    }

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

    private static INamedTypeSymbol? FindReferencedTypeSymbol(
        Compilation compilation,
        string assemblyName,
        string fullTypeName)
    {
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
            {
                continue;
            }

            if (!string.Equals(assemblySymbol.Identity.Name, assemblyName, StringComparison.Ordinal))
            {
                continue;
            }

            return FindTypeSymbol(assemblySymbol, fullTypeName);
        }

        return null;
    }

    private static INamedTypeSymbol? FindTypeSymbol(IAssemblySymbol assemblySymbol, string fullTypeName)
    {
        var lastDot = fullTypeName.LastIndexOf('.');
        var namespaceName = lastDot >= 0 ? fullTypeName[..lastDot] : string.Empty;
        var typeName = lastDot >= 0 ? fullTypeName[(lastDot + 1)..] : fullTypeName;
        var rootTypeName = TypeNameUtilities.GetRootTypeName(typeName);
        var hasGenericArity = TypeNameUtilities.TryGetOpenGenericArity(typeName, out var arity);

        var currentNamespace = assemblySymbol.GlobalNamespace;

        if (!string.IsNullOrEmpty(namespaceName))
        {
            foreach (var namespaceSegment in namespaceName.Split('.'))
            {
                currentNamespace = currentNamespace.GetNamespaceMembers()
                    .SingleOrDefault(ns => string.Equals(ns.Name, namespaceSegment, StringComparison.Ordinal));

                if (currentNamespace is null)
                {
                    return null;
                }
            }
        }

        return hasGenericArity
            ? currentNamespace.GetTypeMembers(rootTypeName, arity).SingleOrDefault()
            : currentNamespace.GetTypeMembers(typeName).SingleOrDefault();
    }
}
