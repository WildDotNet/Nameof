using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Nameof.Internal.Policies;

internal static class MemberInclusionPolicy
{
    internal static HashSet<string> FilterSymbolMembers(
        INamedTypeSymbol type,
        Compilation compilation)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared || compilation.IsSymbolAccessibleWithin(member, compilation.Assembly))
            {
                continue;
            }

            if (TryGetMemberName(member) is { } memberName)
            {
                names.Add(memberName);
            }
        }

        return names;
    }

    internal static HashSet<string> FilterReflectedMembers(
        INamedTypeSymbol? typeSymbol,
        HashSet<string> reflectedNames,
        Compilation compilation)
    {
        FilterReflectionOnlyArtifacts(reflectedNames);

        if (typeSymbol is null)
        {
            return reflectedNames;
        }

        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            var enumFieldNames = new HashSet<string>(
                typeSymbol.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(static field => !field.IsImplicitlyDeclared && field.AssociatedSymbol is null)
                    .Select(static field => field.Name),
                StringComparer.Ordinal);

            reflectedNames.IntersectWith(enumFieldNames);
            return reflectedNames;
        }

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
            {
                continue;
            }

            if (TryGetMemberName(member) is { } name &&
                compilation.IsSymbolAccessibleWithin(member, compilation.Assembly))
            {
                reflectedNames.Remove(name);
            }
        }

        return reflectedNames;
    }

    private static string? TryGetMemberName(ISymbol member)
    {
        if (IsExplicitInterfaceImplementation(member))
        {
            return null;
        }

        var name = member switch
        {
            IFieldSymbol field when field.AssociatedSymbol is null => field.Name,
            IPropertySymbol property => property.Name,
            IEventSymbol @event => @event.Name,
            IMethodSymbol method when method.MethodKind == MethodKind.Ordinary => method.Name,
            _ => null
        };

        return name is not null &&
               !string.IsNullOrWhiteSpace(name) &&
               !name.StartsWith("<", StringComparison.Ordinal)
            ? name
            : null;
    }

    private static bool IsExplicitInterfaceImplementation(ISymbol member)
    {
        return member switch
        {
            IMethodSymbol method => method.ExplicitInterfaceImplementations.Length > 0,
            IPropertySymbol property => property.ExplicitInterfaceImplementations.Length > 0,
            IEventSymbol @event => @event.ExplicitInterfaceImplementations.Length > 0,
            _ => false,
        };
    }

    internal static HashSet<string> ExtractReflectedMembers(Type type, bool includePublicMembers, bool declaredOnly)
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
            AddReflectedMemberIfRelevant(field.Name, names);
        }

        foreach (var property in type.GetProperties(flags))
        {
            AddReflectedMemberIfRelevant(property.Name, names);
        }

        foreach (var @event in type.GetEvents(flags))
        {
            AddReflectedMemberIfRelevant(@event.Name, names);
        }

        foreach (var method in type.GetMethods(flags))
        {
            if (!method.IsSpecialName)
            {
                AddReflectedMemberIfRelevant(method.Name, names);
            }
        }

        return names;
    }

    private static void FilterReflectionOnlyArtifacts(HashSet<string> memberNames)
    {
        memberNames.Remove("value__");
        memberNames.RemoveWhere(static name =>
            name.StartsWith("<", StringComparison.Ordinal) ||
            name.Contains(".", StringComparison.Ordinal) ||
            name.StartsWith("get_", StringComparison.Ordinal) ||
            name.StartsWith("set_", StringComparison.Ordinal) ||
            name.StartsWith("add_", StringComparison.Ordinal) ||
            name.StartsWith("remove_", StringComparison.Ordinal) ||
            name is ".ctor" or ".cctor");
    }

    private static void AddReflectedMemberIfRelevant(string name, HashSet<string> names)
    {
        if (!name.StartsWith("<", StringComparison.Ordinal) &&
            !name.Contains(".", StringComparison.Ordinal))
        {
            names.Add(name);
        }
    }
}
