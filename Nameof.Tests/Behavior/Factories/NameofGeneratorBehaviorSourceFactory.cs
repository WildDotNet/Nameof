using Nameof.Tests.Behavior.Model;

namespace Nameof.Tests.Behavior.Factories;

internal static class NameofGeneratorBehaviorSourceFactory
{
    internal static string CreateCurrentAssemblySource(
        LookupKind lookupKind,
        DeclarationType declarationType,
        AccessType accessType)
    {
        var typeName = $"CurrentAssembly{accessType}{declarationType}";
        var anchorTypeName = $"{typeName}Anchor";

        var visibility = accessType switch
        {
            AccessType.Public => "public",
            AccessType.Internal => "internal",
            _ => throw new ArgumentOutOfRangeException(nameof(accessType)),
        };

        var declaration = declarationType switch
        {
            DeclarationType.Class => CreateClassDeclaration(visibility, typeName),
            DeclarationType.Struct => CreateStructDeclaration(visibility, typeName),
            DeclarationType.Interface => CreateInterfaceDeclaration(visibility, typeName),
            DeclarationType.Enum => CreateEnumDeclaration(visibility, typeName),
            _ => throw new ArgumentOutOfRangeException(nameof(declarationType)),
        };

        return lookupKind switch
        {
            LookupKind.ByType => CreateCurrentAssemblyByTypeSource(typeName, declaration),
            LookupKind.ByAssemblyName => CreateCurrentAssemblyByAssemblyNameSource(typeName, declaration),
            LookupKind.ByAssemblyOf => CreateCurrentAssemblyByAssemblyOfSource(typeName, anchorTypeName, declaration),
            _ => throw new ArgumentOutOfRangeException(nameof(lookupKind)),
        };
    }

    internal static string CreateExternalDeclarationAssemblySource(
        DeclarationType declarationType,
        AccessType accessType,
        string typeName,
        bool includeAnchor,
        string? anchorTypeName)
    {
        var visibility = accessType switch
        {
            AccessType.Public => "public",
            AccessType.Internal => "internal",
            _ => throw new ArgumentOutOfRangeException(nameof(accessType)),
        };

        var internalVisibleToDeclaration = accessType is AccessType.Internal
            ? @$"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""{typeName}.Tests"")]"
            : string.Empty;

        var anchorDeclaration = includeAnchor
            ? @$"public sealed class {anchorTypeName} {{ }}"
            : string.Empty;

        var declaration = declarationType switch
        {
            DeclarationType.Class => CreateClassDeclaration(visibility, typeName),
            DeclarationType.Struct => CreateStructDeclaration(visibility, typeName),
            DeclarationType.Interface => CreateInterfaceDeclaration(visibility, typeName),
            DeclarationType.Enum => CreateEnumDeclaration(visibility, typeName),
            _ => throw new ArgumentOutOfRangeException(nameof(declarationType)),
        };

        return
            $$"""
            {{internalVisibleToDeclaration}}

            namespace ExternalFixtures;

            {{anchorDeclaration}}

            {{declaration}}
            """;
    }

    internal static string CreateDecoyDeclaration(DeclarationType declarationType, string typeName)
    {
        return declarationType switch
        {
            DeclarationType.Class =>
                $$"""
                internal class {{typeName}}
                {
                    internal int Unexpected { get; }
                }
                """,

            DeclarationType.Struct =>
                $$"""
                internal struct {{typeName}}
                {
                    internal int Unexpected { get; }
                }
                """,

            DeclarationType.Interface =>
                $$"""
                internal interface {{typeName}}
                {
                    int Unexpected { get; }
                }
                """,

            DeclarationType.Enum =>
                $$"""
                internal enum {{typeName}}
                {
                    Unexpected
                }
                """,

            _ => throw new ArgumentOutOfRangeException(nameof(declarationType)),
        };
    }

    private static string CreateCurrentAssemblyByTypeSource(string typeOfExpression, string declaration)
    {
        return $$"""
        using Nameof;

        [assembly: GenerateNameof(typeof({{typeOfExpression}}))]

        {{declaration}}
        """;
    }

    private static string CreateCurrentAssemblyByAssemblyNameSource(string typeName, string declaration)
    {
        return $$"""
        using Nameof;

        [assembly: GenerateNameof("{{typeName}}", assemblyName: "GeneratorTests")]

        {{declaration}}
        """;
    }

    private static string CreateCurrentAssemblyByAssemblyOfSource(
        string typeName,
        string anchorTypeName,
        string declaration)
    {
        return $$"""
        using Nameof;

        [assembly: GenerateNameof("{{typeName}}", assemblyOf: typeof({{anchorTypeName}}))]

        public sealed class {{anchorTypeName}};

        {{declaration}}
        """;
    }

    private static string CreateClassDeclaration(string visibility, string typeName)
    {
        return
            $$"""
            {{visibility}} class {{typeName}}
            {
                private int _privateField;
                internal int _internalField;
                protected int _protectedField;
                protected internal int _protectedInternalField;
                private protected int _privateProtectedField;
                public int _publicField;

                private int PrivateProperty { get; set; }
                internal int InternalProperty { get; set; }
                protected int ProtectedProperty { get; set; }
                protected internal int ProtectedInternalProperty { get; set; }
                private protected int PrivateProtectedProperty { get; set; }
                public int PublicProperty { get; set; }

                private event global::System.Action? PrivateEvent;
                internal event global::System.Action? InternalEvent;
                protected event global::System.Action? ProtectedEvent;
                protected internal event global::System.Action? ProtectedInternalEvent;
                private protected event global::System.Action? PrivateProtectedEvent;
                public event global::System.Action? PublicEvent;

                private void PrivateMethod() { }
                internal void InternalMethod() { }
                protected void ProtectedMethod() { }
                protected internal void ProtectedInternalMethod() { }
                private protected void PrivateProtectedMethod() { }
                public void PublicMethod() { }
            }
            """;
    }

    private static string CreateStructDeclaration(string visibility, string typeName)
    {
        return
            $$"""
            {{visibility}} struct {{typeName}}
            {
                private int _privateField;
                internal int _internalField;
                public int _publicField;

                private int PrivateProperty { get; set; }
                internal int InternalProperty { get; set; }
                public int PublicProperty { get; set; }

                private event global::System.Action? PrivateEvent;
                internal event global::System.Action? InternalEvent;
                public event global::System.Action? PublicEvent;

                private void PrivateMethod() { }
                internal void InternalMethod() { }
                public void PublicMethod() { }
            }
            """;
    }

    private static string CreateInterfaceDeclaration(string visibility, string typeName)
    {
        return
            $$"""
            {{visibility}} interface {{typeName}}
            {
                private int PrivateProperty => 1;
                protected int ProtectedProperty => 2;
                internal int InternalProperty => 3;
                protected internal int ProtectedInternalProperty => 4;
                private protected int PrivateProtectedProperty => 5;
                public int PublicProperty => 6;

                private event global::System.Action? PrivateEvent { add { } remove { } }
                protected event global::System.Action? ProtectedEvent { add { } remove { } }
                internal event global::System.Action? InternalEvent { add { } remove { } }
                protected internal event global::System.Action? ProtectedInternalEvent { add { } remove { } }
                private protected event global::System.Action? PrivateProtectedEvent { add { } remove { } }
                public event global::System.Action? PublicEvent { add { } remove { } }

                private void PrivateMethod() { }
                protected void ProtectedMethod() { }
                internal void InternalMethod() { }
                protected internal void ProtectedInternalMethod() { }
                private protected void PrivateProtectedMethod() { }
                public void PublicMethod() { }
            }
            """;
    }

    private static string CreateEnumDeclaration(string visibility, string typeName)
    {
        return
            $$"""
            {{visibility}} enum {{typeName}}
            {
                First,
                Second
            }
            """;
    }
}
