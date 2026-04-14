using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Nameof.Internal.Support;

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
        if (type.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        return "<" + string.Join(", ", type.TypeParameters.Select(static p => p.Name)) + ">";
    }

    public static (string TypeKeyword, string SealedKeyword, bool NeedsPrivateConstructor) GetStubKind(INamedTypeSymbol type)
    {
        return type.TypeKind switch
        {
            TypeKind.Enum => ("enum", "", false),
            TypeKind.Interface => ("interface", "", false),
            TypeKind.Struct => ("struct", "", false),
            _ => ("class", " sealed", true)
        };
    }

    public static string GetTypeIdentity(INamedTypeSymbol type)
    {
        var builder = new StringBuilder();

        if (type.ContainingNamespace is { IsGlobalNamespace: false })
        {
            builder.Append(MakeId(type.ContainingNamespace.ToDisplayString()));
            builder.Append('_');
        }

        var stack = new Stack<INamedTypeSymbol>();
        for (var current = type; current is not null; current = current.ContainingType)
        {
            stack.Push(current);
        }

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            builder.Append(MakeId(current.MetadataName.Replace('`', '_')));
            builder.Append('_');
        }

        return builder.ToString().TrimEnd('_');
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
