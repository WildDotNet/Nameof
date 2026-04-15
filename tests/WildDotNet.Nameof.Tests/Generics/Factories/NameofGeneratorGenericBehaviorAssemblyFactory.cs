using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WildDotNet.Nameof.Tests.Generics.Model;

namespace WildDotNet.Nameof.Tests.Generics.Factories;

internal static class NameofGeneratorGenericBehaviorAssemblyFactory
{
    internal static ExternalFixture CreateExternalFixture(
        string assemblyName,
        GenericDeclarationType declarationType,
        AccessType accessType,
        Arity arity,
        string typeName,
        bool includeAnchor = false,
        string? anchorTypeName = null)
    {
        var source = NameofGeneratorGenericBehaviorSourceFactory.CreateExternalDeclarationAssemblySource(
            declarationType,
            accessType,
            typeName,
            arity,
            includeAnchor,
            anchorTypeName);

        return new ExternalFixture(
            Reference: CreateExternalReferenceAssembly(assemblyName, source),
            TypeName: typeName,
            AssemblyName: assemblyName);
    }

    internal static ExternalFixture CreateCoexistenceFixture(
        string assemblyName,
        AccessType nonGenericAccessType,
        AccessType genericAccessType,
        string typeName,
        bool includeAnchor = false,
        string? anchorTypeName = null)
    {
        var source = NameofGeneratorGenericBehaviorSourceFactory.CreateExternalCoexistenceAssemblySource(
            nonGenericAccessType,
            genericAccessType,
            typeName,
            includeAnchor,
            anchorTypeName);

        return new ExternalFixture(
            Reference: CreateExternalReferenceAssembly(assemblyName, source),
            TypeName: typeName,
            AssemblyName: assemblyName);
    }

    internal static MetadataReference CreateDecoyFixture(
        string assemblyName,
        GenericDeclarationType declarationType,
        AccessType accessType,
        Arity arity,
        string typeName)
    {
        var source = NameofGeneratorGenericBehaviorSourceFactory.CreateExternalDeclarationAssemblySource(
            declarationType,
            accessType,
            typeName,
            arity,
            includeAnchor: false,
            anchorTypeName: null);

        return CreateExternalReferenceAssembly(assemblyName, source);
    }

    internal static MetadataReference CreateCoexistenceDecoyFixture(
        string assemblyName,
        AccessType nonGenericAccessType,
        AccessType genericAccessType,
        string typeName)
    {
        var source = NameofGeneratorGenericBehaviorSourceFactory.CreateExternalCoexistenceDecoyAssemblySource(
            genericAccessType,
            typeName);

        return CreateExternalReferenceAssembly(assemblyName, source);
    }

    private static MetadataReference CreateExternalReferenceAssembly(string assemblyName, string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            GetExternalFixtureReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream);

        Assert.True(
            emitResult.Success,
            string.Join(Environment.NewLine, emitResult.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var image = peStream.ToArray();
        Assembly.Load(image);

        return MetadataReference.CreateFromImage(image);
    }

    private static MetadataReference[] GetExternalFixtureReferences()
    {
        return AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies
            ? trustedPlatformAssemblies
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(static path => MetadataReference.CreateFromFile(path))
                .ToArray()
            : [];
    }
}
