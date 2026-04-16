using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace WildDotNet.Nameof.Internal.Support;

internal static class TypeNameUtilities
{
    public static (string? NamespaceName, string TypeName) SplitNamespaceAndTypeName(string fullTypeName)
    {
        var lastDot = fullTypeName.LastIndexOf('.');
        if (lastDot < 0)
        {
            return (null, fullTypeName);
        }

        return (fullTypeName[..lastDot], fullTypeName[(lastDot + 1)..]);
    }

    public static string FormatTypeParameters(INamedTypeSymbol type)
    {
        var parameterList = GetTypeParameterList(type);
        return parameterList.Length == 0 ? string.Empty : $"<{parameterList}>";
    }

    public static string FormatTypeParameters(Type type)
    {
        var parameterList = GetTypeParameterList(type);
        return parameterList.Length == 0 ? string.Empty : $"<{parameterList}>";
    }

    public static string GetTypeParameterList(INamedTypeSymbol type)
    {
        return string.Join(", ", type.TypeParameters.Select(static p => p.Name));
    }

    public static string GetTypeParameterList(Type type)
    {
        return string.Join(", ", type.GetGenericArguments().Select(static argument => argument.Name));
    }

    public static string GetMetadataFullName(INamedTypeSymbol type)
    {
        var builder = new StringBuilder();

        if (type.ContainingNamespace is { IsGlobalNamespace: false })
        {
            builder.Append(type.ContainingNamespace.ToDisplayString());
            builder.Append('.');
        }

        var containingTypes = new Stack<INamedTypeSymbol>();
        for (var current = type.ContainingType; current is not null; current = current.ContainingType)
        {
            containingTypes.Push(current);
        }

        while (containingTypes.Count > 0)
        {
            builder.Append(containingTypes.Pop().MetadataName);
            builder.Append('+');
        }

        builder.Append(type.MetadataName);
        return builder.ToString();
    }

    public static string GetTypeIdentity(string fullTypeName)
    {
        var (namespaceName, typeName) = SplitNamespaceAndTypeName(fullTypeName);
        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            builder.Append(MakeId(namespaceName!));
            builder.Append('_');
        }

        var containingTypes = typeName.Split('+');
        for (var i = 0; i < containingTypes.Length; i++)
        {
            builder.Append(MakeId(containingTypes[i].Replace('`', '_')));
            if (i < containingTypes.Length - 1)
            {
                builder.Append('_');
            }
        }

        return builder.ToString();
    }

    public static bool IsClosedConstructedGenericType(INamedTypeSymbol type)
    {
        return type.IsGenericType &&
               !type.IsUnboundGenericType &&
               !SymbolEqualityComparer.Default.Equals(type, type.OriginalDefinition);
    }

    public static bool IsOpenGenericDefinition(INamedTypeSymbol type)
    {
        return type.IsGenericType && !IsClosedConstructedGenericType(type);
    }

    public static bool TryGetOpenGenericArity(string fullTypeName, out int arity)
    {
        arity = 0;

        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return false;
        }

        if (fullTypeName.IndexOf('+') >= 0 ||
            fullTypeName.IndexOf('[') >= 0 ||
            fullTypeName.IndexOf(']') >= 0)
        {
            return false;
        }

        var (_, typeName) = SplitNamespaceAndTypeName(fullTypeName);
        var tickIndex = typeName.LastIndexOf('`');
        if (tickIndex <= 0 || tickIndex == typeName.Length - 1)
        {
            return false;
        }

        return int.TryParse(typeName[(tickIndex + 1)..], out arity) && arity > 0;
    }

    public static bool IsClosedGenericTypeName(string fullTypeName)
    {
        return !string.IsNullOrWhiteSpace(fullTypeName) &&
               (fullTypeName.IndexOf('[') >= 0 || fullTypeName.IndexOf(']') >= 0);
    }

    public static string GetRootTypeName(string typeName)
    {
        var tickIndex = typeName.LastIndexOf('`');
        return tickIndex > 0 ? typeName[..tickIndex] : typeName;
    }

    public static string GetRootMetadataFullName(string fullTypeName)
    {
        var (namespaceName, typeName) = SplitNamespaceAndTypeName(fullTypeName);
        var rootTypeName = GetRootTypeName(typeName);
        return string.IsNullOrEmpty(namespaceName)
            ? rootTypeName
            : $"{namespaceName}.{rootTypeName}";
    }

    public static string GetRootTypeName(Type type)
    {
        return GetRootTypeName(type.Name);
    }

    public static string GetTypeIdentity(INamedTypeSymbol type)
    {
        return GetTypeIdentity(GetMetadataFullName(type));
    }

    public static string MakeId(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
        }

        return builder.ToString();
    }
}
