using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nameof.Tests.Behavior.Model;

namespace Nameof.Tests.Behavior.Factories;

internal static class NameofGeneratorBehaviorAssemblyFactory
{
    internal static ExternalFixture CreateExternalFixture(
        string assemblyName,
        DeclarationType declarationType,
        AccessType accessType,
        string typeName,
        bool includeAnchor = false,
        string? anchorTypeName = null)
    {
        var source = NameofGeneratorBehaviorSourceFactory.CreateExternalDeclarationAssemblySource(
            declarationType,
            accessType,
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
        DeclarationType declarationType,
        string typeName)
    {
        var declaration = NameofGeneratorBehaviorSourceFactory.CreateDecoyDeclaration(declarationType, typeName);
        var source =
            $$"""
            namespace ExternalFixtures;

            {{declaration}}
            """;

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
