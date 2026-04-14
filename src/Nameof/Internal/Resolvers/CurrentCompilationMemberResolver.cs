using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Nameof.Internal.Generation;
using Nameof.Internal.Model;

namespace Nameof.Internal.Resolvers;

internal sealed class CurrentCompilationMemberResolver : ITypeMemberResolver
{
    public bool CanResolve(NameofRequest request, Compilation compilation)
    {
        if (request.Symbol is not null)
        {
            return SymbolEqualityComparer.Default.Equals(request.Symbol.ContainingAssembly, compilation.Assembly);
        }

        if (request.FullTypeName is null)
        {
            return false;
        }

        var requestedAssemblyName = request.AssemblyOfType?.ContainingAssembly.Identity.Name ?? request.AssemblyName;
        return string.Equals(requestedAssemblyName, compilation.Assembly.Identity.Name, System.StringComparison.Ordinal);
    }

    public ResolvedNameofType? Resolve(NameofRequest request, Compilation compilation)
    {
        var type = request.Symbol ?? compilation.GetTypeByMetadataName(request.FullTypeName!);
        if (type is null)
        {
            return null;
        }

        var includePublicMembers = type.DeclaredAccessibility != Accessibility.Public;
        var memberNames = ExtractMemberNames(type, includePublicMembers);
        return NameofSourceEmitter.CreateResolvedSymbolType(type, memberNames);
    }

    private static HashSet<string> ExtractMemberNames(INamedTypeSymbol type, bool includePublicMembers)
    {
        var names = new HashSet<string>(System.StringComparer.Ordinal);

        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
            {
                continue;
            }

            if (!includePublicMembers && member.DeclaredAccessibility == Accessibility.Public)
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

            if (!string.IsNullOrWhiteSpace(name) && name is not null && !name.StartsWith("<", System.StringComparison.Ordinal))
            {
                names.Add(name);
            }
        }

        return names;
    }
}
