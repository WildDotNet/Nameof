namespace Nameof.Internal.Generation;

internal static class NameofCoreSource
{
    public const string Text =
        """
        #nullable enable

        namespace Nameof
        {
            public static class nameof<T>
            {
            }

            [global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]
            internal sealed class GenerateNameofAttribute : global::System.Attribute
            {
                public GenerateNameofAttribute(global::System.Type type) { }
                public GenerateNameofAttribute(string fullTypeName, global::System.Type assemblyOf) { }
                public GenerateNameofAttribute(string fullTypeName, string assemblyName) { }
            }

            [global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]
            internal sealed class GenerateNameofAttribute<T> : global::System.Attribute
            {
            }
        }
        """;
}
