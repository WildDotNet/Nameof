using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Nameof.Internal.Generation;
using Nameof.Internal.Model;

namespace Nameof.Internal.Resolvers;

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
            var includePublicMembers = request.Symbol.DeclaredAccessibility != Accessibility.Public;
            var fullTypeName = request.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);
            var assemblyName = request.Symbol.ContainingAssembly.Identity.Name;

            var runtimeType = TryFindLoadedType(fullTypeName) ??
                TryLoadTypeFromReferences(compilation, fullTypeName) ??
                Type.GetType($"{fullTypeName}, {assemblyName}", throwOnError: false) ??
                TryLoadAssemblyFromReferences(compilation, assemblyName)?.GetType(fullTypeName, throwOnError: false) ??
                TryLoadAssemblyByName(assemblyName)?.GetType(fullTypeName, throwOnError: false);

            return runtimeType is null
                ? null
                : NameofSourceEmitter.CreateResolvedSymbolType(
                    request.Symbol,
                    ExtractMemberNames(runtimeType, includePublicMembers, declaredOnly: true));
        }

        if (request.FullTypeName is null)
        {
            return null;
        }

        if (request.AssemblyOfType is not null)
        {
            var assemblyName = request.AssemblyOfType.ContainingAssembly.Identity.Name;
            var assemblyOfFullName = request.AssemblyOfType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", string.Empty);

            var whereRuntimeType = TryFindLoadedType(assemblyOfFullName) ??
                Type.GetType($"{assemblyOfFullName}, {assemblyName}", throwOnError: false) ??
                TryLoadAssemblyByName(assemblyName)?.GetType(assemblyOfFullName, throwOnError: false);

            var targetAssembly = whereRuntimeType?.Assembly ?? TryLoadAssemblyByName(assemblyName);
            var resolved = targetAssembly?.GetType(request.FullTypeName, throwOnError: false);

            return resolved is null
                ? null
                : CreateResolvedFullTypeRequest(
                    resolved,
                    ExtractMemberNames(resolved, includePublicMembers: true, declaredOnly: false));
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

        return type is null
            ? null
            : CreateResolvedFullTypeRequest(
                type,
                ExtractMemberNames(type, includePublicMembers: true, declaredOnly: false));
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

    private static ResolvedNameofType CreateResolvedFullTypeRequest(Type type, HashSet<string> memberNames)
    {
        var fullTypeName = type.FullName
            ?? throw new InvalidOperationException("Resolved external type must have a full name.");
        var (namespaceName, typeName) = Nameof.Internal.Support.TypeNameUtilities.SplitNamespaceAndTypeName(fullTypeName);

        return new ResolvedNameofType(
            fullTypeName,
            namespaceName,
            typeName,
            EmitStub: true,
            WrapperClassName: "Nameof_" + Nameof.Internal.Support.TypeNameUtilities.MakeId(fullTypeName),
            FullyQualifiedTypeName: $"global::{fullTypeName}",
            MemberNames: memberNames,
            StubKind: GetStubKind(type));
    }

    private static (string TypeKeyword, string SealedKeyword, bool NeedsPrivateConstructor) GetStubKind(Type type)
    {
        if (type.IsEnum)
        {
            return ("enum", "", false);
        }

        if (type.IsInterface)
        {
            return ("interface", "", false);
        }

        if (type.IsValueType)
        {
            return ("struct", "", false);
        }

        return ("class", " sealed", true);
    }
}
