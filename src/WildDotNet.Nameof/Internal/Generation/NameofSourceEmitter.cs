using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Internal.Model;
using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof.Internal.Generation;

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
            OpenAnnotatedBlock(writer, $"internal{stubKind.SealedKeyword} {stubKind.TypeKeyword} {resolved.TypeName}{resolved.TypeParameters}");

            if (stubKind.NeedsPrivateConstructor)
            {
                writer.Line($"private {resolved.TypeName}() {{ }}");
            }

            writer.CloseBlock();
            writer.Line();
        }

        OpenAnnotatedBlock(writer, $"internal static class {resolved.WrapperClassName}");
        if (resolved.IsOpenGenericDefinition)
        {
            writer.OpenBlock($"extension({GeneratorConstants.FullyQualifiedNamespace}.nameof<{resolved.ExtensionTargetFullyQualifiedTypeName}>)");
            writer.Line($"internal static {GeneratorConstants.FullyQualifiedNamespace}.NameofGeneric<{resolved.ExtensionTargetFullyQualifiedTypeName}, TArity> of<TArity>() where TArity : {GeneratorConstants.FullyQualifiedNamespace}.INameofGenericArity{resolved.GenericArity} => {GeneratorConstants.FullyQualifiedNamespace}.NameofGeneric<{resolved.ExtensionTargetFullyQualifiedTypeName}, TArity>.Instance;");
            writer.CloseBlock();
            writer.Line();

            writer.OpenBlock($"extension({GeneratorConstants.FullyQualifiedNamespace}.NameofGeneric<{resolved.ExtensionTargetFullyQualifiedTypeName}, {GeneratorConstants.FullyQualifiedNamespace}.arity{resolved.GenericArity}> _)");
            WriteMemberProperties(writer, useStaticAccessibility: false, resolved.MemberNames);
            writer.CloseBlock();
        }
        else
        {
            writer.OpenBlock(GetExtensionTarget(resolved));
            WriteMemberProperties(writer, useStaticAccessibility: true, resolved.MemberNames);
            writer.CloseBlock();
        }
        writer.CloseBlock();

        if (!string.IsNullOrWhiteSpace(resolved.NamespaceName))
        {
            writer.CloseBlock();
        }

        return writer.ToString();
    }

    private static void OpenAnnotatedBlock(CodeWriter writer, string header)
    {
        writer.Line("[global::Microsoft.CodeAnalysis.Embedded]");
        writer.OpenBlock(header);
    }

    private static void WriteMemberProperties(
        CodeWriter writer,
        bool useStaticAccessibility,
        System.Collections.Generic.IReadOnlyCollection<string> memberNames)
    {
        var memberPrefix = useStaticAccessibility ? "public static" : "public";

        foreach (var (identifier, value) in IdentifierUtilities.BuildMemberMap(memberNames)
                     .OrderBy(static m => m.Identifier, StringComparer.Ordinal))
        {
            writer.Line($"{memberPrefix} string {identifier} => \"{IdentifierUtilities.EscapeStringLiteral(value)}\";");
        }
    }

    public static ResolvedNameofType? CreateResolvedSymbolType(
        Compilation compilation,
        INamedTypeSymbol type,
        System.Collections.Generic.HashSet<string> memberNames,
        bool isOpenGenericDefinition)
    {
        if (memberNames.Count == 0)
        {
            return null;
        }

        return isOpenGenericDefinition
            ? CreateResolvedOpenGenericSymbolType(compilation, type, memberNames)
            : CreateResolvedNonGenericSymbolType(type, memberNames);
    }

    public static ResolvedNameofType? CreateResolvedRuntimeType(
        Compilation compilation,
        Type type,
        System.Collections.Generic.HashSet<string> memberNames,
        bool isOpenGenericDefinition)
    {
        if (memberNames.Count == 0)
        {
            return null;
        }

        return isOpenGenericDefinition
            ? CreateResolvedOpenGenericRuntimeType(compilation, type, memberNames)
            : CreateResolvedNonGenericRuntimeType(compilation, type, memberNames);
    }

    private static ResolvedNameofType CreateResolvedNonGenericSymbolType(
        INamedTypeSymbol type,
        System.Collections.Generic.HashSet<string> memberNames)
    {
        var containingNamespace = type.ContainingNamespace is { IsGlobalNamespace: false }
            ? type.ContainingNamespace.ToDisplayString()
            : null;

        var needsStub = type.DeclaredAccessibility != Accessibility.Public && !type.Locations.Any(static l => l.IsInSource);
        var fullyQualifiedTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new ResolvedNameofType(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty),
            containingNamespace,
            type.Name,
            EmitStub: needsStub,
            StubIdentity: needsStub ? GetStubIdentity(containingNamespace, type.Name) : null,
            WrapperClassName: "Nameof_" + TypeNameUtilities.GetTypeIdentity(type),
            ExtensionTargetFullyQualifiedTypeName: fullyQualifiedTypeName,
            MemberNames: memberNames,
            TypeParameters: TypeNameUtilities.FormatTypeParameters(type),
            StubKind: needsStub ? TypeNameUtilities.GetStubKind(type) : null);
    }

    private static ResolvedNameofType CreateResolvedOpenGenericSymbolType(
        Compilation compilation,
        INamedTypeSymbol type,
        System.Collections.Generic.HashSet<string> memberNames)
    {
        var metadataFullName = TypeNameUtilities.GetMetadataFullName(type);
        var rootMetadataFullName = TypeNameUtilities.GetRootMetadataFullName(metadataFullName);
        var rootSymbol = TypeNameUtilities.FindOpenGenericRootSymbol(type)
            ?? compilation.GetTypeByMetadataName(rootMetadataFullName);
        var needsStub = rootSymbol is null || NeedsStub(rootSymbol);
        var containingNamespace = type.ContainingNamespace is { IsGlobalNamespace: false }
            ? type.ContainingNamespace.ToDisplayString()
            : null;
        var rootTypeName = type.Name;

        return new ResolvedNameofType(
            metadataFullName,
            containingNamespace,
            rootTypeName,
            EmitStub: needsStub,
            StubIdentity: needsStub ? GetStubIdentity(containingNamespace, rootTypeName) : null,
            WrapperClassName: "NameofGeneric_" + TypeNameUtilities.GetTypeIdentity(type),
            ExtensionTargetFullyQualifiedTypeName: needsStub
                ? $"global::{rootMetadataFullName}"
                : rootSymbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MemberNames: memberNames,
            IsOpenGenericDefinition: true,
            GenericArity: type.Arity,
            StubKind: needsStub
                ? rootSymbol is not null
                    ? TypeNameUtilities.GetStubKind(rootSymbol)
                    : TypeNameUtilities.GetStubKind(type)
                : null);
    }

    private static ResolvedNameofType CreateResolvedNonGenericRuntimeType(
        Compilation compilation,
        Type type,
        System.Collections.Generic.HashSet<string> memberNames)
    {
        var fullTypeName = type.FullName
            ?? throw new InvalidOperationException("Resolved external type must have a full name.");
        var (namespaceName, typeName) = TypeNameUtilities.SplitNamespaceAndTypeName(fullTypeName);
        var hasSymbolInCompilation = compilation.GetTypeByMetadataName(fullTypeName) is not null;

        return new ResolvedNameofType(
            fullTypeName,
            namespaceName,
            typeName,
            EmitStub: !hasSymbolInCompilation,
            StubIdentity: !hasSymbolInCompilation ? GetStubIdentity(namespaceName, typeName) : null,
            WrapperClassName: "Nameof_" + TypeNameUtilities.MakeId(fullTypeName),
            ExtensionTargetFullyQualifiedTypeName: $"global::{fullTypeName}",
            MemberNames: memberNames,
            StubKind: !hasSymbolInCompilation ? GetStubKind(type) : null);
    }

    private static ResolvedNameofType CreateResolvedOpenGenericRuntimeType(
        Compilation compilation,
        Type type,
        System.Collections.Generic.HashSet<string> memberNames)
    {
        var fullTypeName = type.FullName
            ?? throw new InvalidOperationException("Resolved external generic type must have a full name.");
        var rootMetadataFullName = TypeNameUtilities.GetRootMetadataFullName(fullTypeName);
        var rootSymbol = compilation.GetTypeByMetadataName(rootMetadataFullName);
        var (namespaceName, _) = TypeNameUtilities.SplitNamespaceAndTypeName(fullTypeName);
        var rootTypeName = TypeNameUtilities.GetRootTypeName(type);
        var needsStub = rootSymbol is null || NeedsStub(rootSymbol);

        return new ResolvedNameofType(
            fullTypeName,
            namespaceName,
            rootTypeName,
            EmitStub: needsStub,
            StubIdentity: needsStub ? GetStubIdentity(namespaceName, rootTypeName) : null,
            WrapperClassName: "NameofGeneric_" + TypeNameUtilities.MakeId(fullTypeName.Replace('`', '_')),
            ExtensionTargetFullyQualifiedTypeName: needsStub
                ? $"global::{rootMetadataFullName}"
                : rootSymbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MemberNames: memberNames,
            IsOpenGenericDefinition: true,
            GenericArity: type.GetGenericArguments().Length,
            StubKind: needsStub
                ? rootSymbol is not null
                    ? TypeNameUtilities.GetStubKind(rootSymbol)
                    : GetStubKind(type)
                : null);
    }

    private static bool NeedsStub(INamedTypeSymbol type)
    {
        return type.DeclaredAccessibility != Accessibility.Public && !type.Locations.Any(static l => l.IsInSource);
    }

    private static string GetExtensionTarget(ResolvedNameofType resolved)
    {
        return $"extension({GeneratorConstants.FullyQualifiedNamespace}.nameof<{resolved.ExtensionTargetFullyQualifiedTypeName}>)";
    }

    private static string GetStubIdentity(string? namespaceName, string typeName)
    {
        return string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}";
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
