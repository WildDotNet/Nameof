namespace Nameof.Internal.Generation;

internal static class NameofCoreSource
{
    private const string EmbeddedAnnotation = "[global::Microsoft.CodeAnalysis.Embedded]";

    public const string Text =
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

        namespace Nameof
        {
            {{EmbeddedAnnotation}}
            public static class nameof<T>
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
}
