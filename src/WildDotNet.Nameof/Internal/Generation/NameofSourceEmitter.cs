using System;
using System.Collections.Generic;
using System.Linq;
using WildDotNet.Nameof.Internal.Model;
using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof.Internal.Generation;

internal static class NameofSourceEmitter
{
    public static string Render(EmissionPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.WrapperClassName))
        {
            return string.Empty;
        }

        var writer = new CodeWriter();
        writer.Line("#nullable enable");
        writer.Line();

        if (!string.IsNullOrWhiteSpace(plan.NamespaceName))
        {
            writer.OpenBlock($"namespace {plan.NamespaceName}");
        }

        if (plan.Stub is { } stub)
        {
            OpenAnnotatedBlock(writer, $"internal{stub.Kind.SealedKeyword} {stub.Kind.TypeKeyword} {stub.TypeName}{stub.TypeParameters}");

            if (stub.Kind.NeedsPrivateConstructor)
            {
                writer.Line($"private {stub.TypeName}() {{ }}");
            }

            writer.CloseBlock();
            writer.Line();
        }

        OpenAnnotatedBlock(writer, $"internal static class {plan.WrapperClassName}");
        var @namespace = GeneratorConstants.FullyQualifiedNamespace;
        var target = plan.ExtensionTargetFullyQualifiedTypeName;

        if (plan.IsOpenGenericDefinition)
        {
            var arity = plan.GenericArity;

            writer.OpenBlock($"extension({@namespace}.nameof<{target}>)");
            writer.Line($"internal static {@namespace}.NameofGeneric<{target}, TArity> of<TArity>() where TArity : {@namespace}.INameofGenericArity{arity} => {@namespace}.NameofGeneric<{target}, TArity>.Instance;");
            writer.CloseBlock();
            writer.Line();

            writer.OpenBlock($"extension({@namespace}.NameofGeneric<{target}, {@namespace}.arity{arity}> _)");
            WriteMemberProperties(writer, useStaticAccessibility: false, plan.MemberNames);
            writer.CloseBlock();
        }
        else
        {
            writer.OpenBlock($"extension({@namespace}.nameof<{target}>)");
            WriteMemberProperties(writer, useStaticAccessibility: true, plan.MemberNames);
            writer.CloseBlock();
        }

        writer.CloseBlock();

        if (!string.IsNullOrWhiteSpace(plan.NamespaceName))
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
        IReadOnlyCollection<string> memberNames)
    {
        var memberPrefix = useStaticAccessibility ? "public static" : "public";

        foreach (var (identifier, value) in IdentifierUtilities.BuildMemberMap(memberNames)
                     .OrderBy(static m => m.Identifier, StringComparer.Ordinal))
        {
            writer.Line($"{memberPrefix} string {identifier} => \"{IdentifierUtilities.EscapeStringLiteral(value)}\";");
        }
    }
}
