using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Nameof.Internal.Model;
using Nameof.Internal.Support;

namespace Nameof.Internal.Generation;

internal static class NameofSourceEmitter
{
    public static string EmitResolvedType(ResolvedNameofType resolved)
    {
        if (string.IsNullOrWhiteSpace(resolved.TypeName))
        {
            return string.Empty;
        }

        var writer = new CodeWriter();
        writer.Line("#nullable enable");
        writer.Line();

        if (!string.IsNullOrWhiteSpace(resolved.NamespaceName))
        {
            writer.OpenBlock($"namespace {resolved.NamespaceName}");
        }

        if (resolved.EmitStub && resolved.StubKind is { } stubKind)
        {
            writer.OpenBlock($"internal{stubKind.SealedKeyword} {stubKind.TypeKeyword} {resolved.TypeName}{resolved.TypeParameters}");

            if (stubKind.NeedsPrivateConstructor)
            {
                writer.Line($"private {resolved.TypeName}() {{ }}");
            }

            writer.CloseBlock();
            writer.Line();
        }

        writer.OpenBlock($"internal static class {resolved.WrapperClassName}");
        writer.OpenBlock($"extension(global::Nameof.nameof<{resolved.FullyQualifiedTypeName}>)");

        foreach (var (identifier, value) in IdentifierUtilities.BuildMemberMap(resolved.MemberNames)
                     .OrderBy(static m => m.Identifier, StringComparer.Ordinal))
        {
            writer.Line($"public static string {identifier} => \"{IdentifierUtilities.EscapeStringLiteral(value)}\";");
        }

        writer.CloseBlock();
        writer.CloseBlock();

        if (!string.IsNullOrWhiteSpace(resolved.NamespaceName))
        {
            writer.CloseBlock();
        }

        return writer.ToString();
    }

    public static ResolvedNameofType CreateResolvedExternalType(
        Compilation compilation,
        string fullTypeName,
        System.Collections.Generic.HashSet<string> memberNames)
    {
        var (namespaceName, typeName) = TypeNameUtilities.SplitNamespaceAndTypeName(fullTypeName);
        var hasSymbolInCompilation = compilation.GetTypeByMetadataName(fullTypeName) is not null;

        return new ResolvedNameofType(
            fullTypeName,
            namespaceName,
            typeName,
            EmitStub: !hasSymbolInCompilation,
            WrapperClassName: "Nameof_" + TypeNameUtilities.MakeId(fullTypeName),
            FullyQualifiedTypeName: $"global::{fullTypeName}",
            MemberNames: memberNames,
            StubKind: !hasSymbolInCompilation ? ("class", " sealed", true) : null);
    }

    public static ResolvedNameofType? CreateResolvedSymbolType(
        INamedTypeSymbol type,
        System.Collections.Generic.HashSet<string> memberNames)
    {
        if (memberNames.Count == 0)
        {
            return null;
        }

        var containingNamespace = type.ContainingNamespace is { IsGlobalNamespace: false }
            ? type.ContainingNamespace.ToDisplayString()
            : null;

        var needsStub = type.DeclaredAccessibility != Accessibility.Public && !type.Locations.Any(static l => l.IsInSource);

        return new ResolvedNameofType(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty),
            containingNamespace,
            type.Name,
            EmitStub: needsStub,
            WrapperClassName: "Nameof_" + TypeNameUtilities.GetTypeIdentity(type),
            FullyQualifiedTypeName: type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MemberNames: memberNames,
            TypeParameters: TypeNameUtilities.FormatTypeParameters(type),
            StubKind: needsStub ? TypeNameUtilities.GetStubKind(type) : null);
    }
}
