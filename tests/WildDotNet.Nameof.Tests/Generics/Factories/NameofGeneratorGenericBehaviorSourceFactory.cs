using WildDotNet.Nameof.Tests.Generics.Model;

namespace WildDotNet.Nameof.Tests.Generics.Factories;

internal static class NameofGeneratorGenericBehaviorSourceFactory
{
    internal static string CreateCurrentAssemblySource(
        LookupKind lookupKind,
        GenericDeclarationType declarationType,
        Arity arity,
        AccessType accessType)
    {
        var typeName = BuildTypeName("CurrentAssembly", accessType, declarationType, arity);
        var anchorTypeName = $"{typeName}Anchor";
        var declaration = CreateGenericDeclaration(declarationType, accessType, typeName, arity);
        var metadataName = GenericArityUtilities.GetMetadataName(typeName, arity);

        return lookupKind switch
        {
            LookupKind.ByType => CreateCurrentAssemblyByTypeSource(typeName, arity, declaration),
            LookupKind.ByAssemblyName => CreateCurrentAssemblyByAssemblyNameSource(metadataName, declaration),
            LookupKind.ByAssemblyOf => CreateCurrentAssemblyByAssemblyOfSource(metadataName, anchorTypeName, declaration),
            _ => throw new ArgumentOutOfRangeException(nameof(lookupKind)),
        };
    }

    internal static string CreateCurrentAssemblyCoexistenceSource(
        LookupKind lookupKind,
        AccessType nonGenericAccessType,
        AccessType genericAccessType)
    {
        const string typeName = "SharedName";
        const string anchorTypeName = "SharedNameAnchor";

        var nonGenericDeclaration = CreateNonGenericClassDeclaration(nonGenericAccessType, typeName);
        var genericDeclaration = CreateGenericDeclaration(GenericDeclarationType.Class, genericAccessType, typeName, Arity.Arity1);
        var metadataName = GenericArityUtilities.GetMetadataName(typeName, Arity.Arity1);

        return lookupKind switch
        {
            LookupKind.ByType => $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof(typeof({{typeName}}<>))]

            {{nonGenericDeclaration}}

            {{genericDeclaration}}
            """,
            LookupKind.ByAssemblyName => $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("{{metadataName}}", assemblyName: "GeneratorTests")]

            {{nonGenericDeclaration}}

            {{genericDeclaration}}
            """,
            LookupKind.ByAssemblyOf => $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("{{metadataName}}", assemblyOf: typeof({{anchorTypeName}}))]

            public sealed class {{anchorTypeName}} { }

            {{nonGenericDeclaration}}

            {{genericDeclaration}}
            """,
            _ => throw new ArgumentOutOfRangeException(nameof(lookupKind)),
        };
    }

    internal static string CreateExternalDeclarationAssemblySource(
        GenericDeclarationType declarationType,
        AccessType accessType,
        string typeName,
        Arity arity,
        bool includeAnchor,
        string? anchorTypeName)
    {
        var internalVisibleToDeclaration = accessType is AccessType.Internal
            ? """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GeneratorTests")]"""
            : string.Empty;

        var anchorDeclaration = includeAnchor
            ? $$"""public sealed class {{anchorTypeName}} { }"""
            : string.Empty;

        var declaration = CreateGenericDeclaration(
            declarationType,
            accessType,
            typeName,
            arity);

        return $$"""
        {{internalVisibleToDeclaration}}

        namespace GenericFixtures;

        {{anchorDeclaration}}

        {{declaration}}
        """;
    }

    internal static string CreateExternalCoexistenceAssemblySource(
        AccessType nonGenericAccessType,
        AccessType genericAccessType,
        string typeName,
        bool includeAnchor,
        string? anchorTypeName)
    {
        var internalVisibleToDeclaration = nonGenericAccessType is AccessType.Internal || genericAccessType is AccessType.Internal
            ? """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GeneratorTests")]"""
            : string.Empty;

        var anchorDeclaration = includeAnchor
            ? $$"""public sealed class {{anchorTypeName}} { }"""
            : string.Empty;

        return $$"""
        {{internalVisibleToDeclaration}}

        namespace GenericFixtures;

        {{anchorDeclaration}}

        {{CreateNonGenericClassDeclaration(nonGenericAccessType, typeName)}}

        {{CreateGenericDeclaration(GenericDeclarationType.Class, genericAccessType, typeName, Arity.Arity1)}}
        """;
    }

    internal static string CreateExternalCoexistenceDecoyAssemblySource(
        AccessType genericAccessType,
        string typeName)
    {
        var internalVisibleToDeclaration = genericAccessType is AccessType.Internal
            ? """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GeneratorTests")]"""
            : string.Empty;

        return $$"""
        {{internalVisibleToDeclaration}}

        namespace GenericFixtures;

        {{CreateGenericDeclaration(GenericDeclarationType.Class, genericAccessType, typeName, Arity.Arity1)}}
        """;
    }

    private static string CreateCurrentAssemblyByTypeSource(string typeName, Arity arity, string declaration)
    {
        return $$"""
        using WildDotNet.Nameof;

        [assembly: GenerateNameof(typeof({{typeName}}{{GenericArityUtilities.GetOpenGenericTypeArguments(arity)}}))]

        {{declaration}}
        """;
    }

    private static string CreateCurrentAssemblyByAssemblyNameSource(string metadataName, string declaration)
    {
        return $$"""
        using WildDotNet.Nameof;

        [assembly: GenerateNameof("{{metadataName}}", assemblyName: "GeneratorTests")]

        {{declaration}}
        """;
    }

    private static string CreateCurrentAssemblyByAssemblyOfSource(
        string metadataName,
        string anchorTypeName,
        string declaration)
    {
        return $$"""
        using WildDotNet.Nameof;

        [assembly: GenerateNameof("{{metadataName}}", assemblyOf: typeof({{anchorTypeName}}))]

        public sealed class {{anchorTypeName}} { }

        {{declaration}}
        """;
    }

    private static string CreateGenericDeclaration(
        GenericDeclarationType declarationType,
        AccessType accessType,
        string typeName,
        Arity arity)
    {
        var visibility = GetVisibility(accessType);
        var typeParameters = GenericArityUtilities.GetTypeParameters(arity);

        return declarationType switch
        {
            GenericDeclarationType.Class => CreateClassDeclaration(visibility, typeName, typeParameters),
            GenericDeclarationType.Struct => CreateStructDeclaration(visibility, typeName, typeParameters),
            GenericDeclarationType.Interface => CreateInterfaceDeclaration(visibility, typeName, typeParameters),
            _ => throw new ArgumentOutOfRangeException(nameof(declarationType)),
        };
    }

    private static string CreateNonGenericClassDeclaration(AccessType accessType, string typeName)
    {
        var visibility = GetVisibility(accessType);

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

    private static string CreateClassDeclaration(string visibility, string typeName, string typeParameters)
    {
        return
            $$"""
            {{visibility}} class {{typeName}}{{typeParameters}}
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

    private static string CreateStructDeclaration(string visibility, string typeName, string typeParameters)
    {
        return
            $$"""
            {{visibility}} struct {{typeName}}{{typeParameters}}
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

    private static string CreateInterfaceDeclaration(string visibility, string typeName, string typeParameters)
    {
        return
            $$"""
            {{visibility}} interface {{typeName}}{{typeParameters}}
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

    private static string GetVisibility(AccessType accessType)
    {
        return accessType switch
        {
            AccessType.Public => "public",
            AccessType.Internal => "internal",
            _ => throw new ArgumentOutOfRangeException(nameof(accessType)),
        };
    }

    private static string BuildTypeName(string prefix, AccessType accessType, GenericDeclarationType declarationType, Arity arity)
    {
        return $"{prefix}{accessType}{declarationType}{arity}";
    }
}
