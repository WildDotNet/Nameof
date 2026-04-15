using System.Collections.Generic;
using System.Linq;
using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof.Internal.Generation;

internal static class NameofCoreSource
{
    private const string EmbeddedAnnotation = "[global::Microsoft.CodeAnalysis.Embedded]";
    private const string ReservedIdentifierSuppression =
        "[global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"Compiler\", \"CS8981\", Justification = \"Generated code.\")]";

    public const string BaseText =
        $$"""
        #nullable enable

        namespace Microsoft.CodeAnalysis
        {
            {{EmbeddedAnnotation}}
            [global::System.AttributeUsage(global::System.AttributeTargets.All, Inherited = false)]
            internal sealed class EmbeddedAttribute : global::System.Attribute
            {
            }
        }

        namespace {{GeneratorConstants.ProductNamespace}}
        {
            {{EmbeddedAnnotation}}
            {{ReservedIdentifierSuppression}}
            internal static class nameof<T>
            {
            }

            {{EmbeddedAnnotation}}
            [global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]
            internal sealed class GenerateNameofAttribute : global::System.Attribute
            {
                public GenerateNameofAttribute(global::System.Type type) { }
                public GenerateNameofAttribute(string fullTypeName, global::System.Type assemblyOf) { }
                public GenerateNameofAttribute(string fullTypeName, string assemblyName) { }
            }
        }
        """;

    public static string CreateGenericSupport(IEnumerable<int> arities)
    {
        var arityList = arities
            .Where(static arity => arity > 0)
            .Distinct()
            .OrderBy(static arity => arity)
            .ToArray();

        if (arityList.Length == 0)
        {
            return string.Empty;
        }

        var writer = new CodeWriter();
        writer.Line("#nullable enable");
        writer.Line();
        writer.OpenBlock($"namespace {GeneratorConstants.ProductNamespace}");
        writer.Line(EmbeddedAnnotation);
        writer.OpenBlock("internal sealed class NameofGeneric<T, TArity>");
        writer.Line("private NameofGeneric() { }");
        writer.Line("internal static NameofGeneric<T, TArity> Instance { get; } = new();");
        writer.CloseBlock();
        writer.Line();

        foreach (var arity in arityList)
        {
            writer.Line(EmbeddedAnnotation);
            writer.OpenBlock($"internal interface INameofGenericArity{arity}");
            writer.CloseBlock();
            writer.Line();

            writer.Line(EmbeddedAnnotation);
            writer.Line(ReservedIdentifierSuppression);
            writer.OpenBlock($"internal sealed class arity{arity} : {GeneratorConstants.FullyQualifiedNamespace}.INameofGenericArity{arity}");
            writer.Line($"private arity{arity}() {{ }}");
            writer.CloseBlock();
            writer.Line();
        }

        writer.CloseBlock();
        return writer.ToString();
    }
}
