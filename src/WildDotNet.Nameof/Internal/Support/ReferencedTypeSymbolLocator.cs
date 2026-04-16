using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace WildDotNet.Nameof.Internal.Support;

internal static class ReferencedTypeSymbolLocator
{
    public static INamedTypeSymbol? FindReferencedTypeSymbol(
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

    public static INamedTypeSymbol? FindTypeSymbol(IAssemblySymbol assemblySymbol, string fullTypeName)
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
