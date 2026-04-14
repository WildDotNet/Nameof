using Microsoft.CodeAnalysis;
using Nameof.Tests.Behavior.Model;

namespace Nameof.Tests.Behavior.Factories;

internal static class NameofGeneratorBehaviorTestCaseFactory
{
    internal static BehaviorCase BuildBehaviorCase(
        AssemblyType assemblyType,
        LookupKind lookupKind,
        DeclarationType declarationType,
        AccessType accessType)
    {
        var snapshotName = $"{assemblyType}_{lookupKind}_{declarationType}_{accessType}";

        return (assemblyType, lookupKind) switch
        {
            (AssemblyType.CurrentAssembly, _)
                => CreateBehaviorCase(
                    snapshotName,
                    NameofGeneratorBehaviorSourceFactory.CreateCurrentAssemblySource(
                        lookupKind,
                        declarationType,
                        accessType)),

            (AssemblyType.ExternalAssembly, LookupKind.ByType)
                => BuildExternalAssemblyByTypeCase(snapshotName, declarationType, accessType),

            (AssemblyType.ExternalAssembly, LookupKind.ByAssemblyName)
                => BuildExternalAssemblyByAssemblyNameCase(snapshotName, declarationType, accessType),

            (AssemblyType.ExternalAssembly, LookupKind.ByAssemblyOf)
                => BuildExternalAssemblyByAssemblyOfCase(snapshotName, declarationType, accessType),

            _ => throw new ArgumentOutOfRangeException(nameof(lookupKind)),
        };
    }

    private static BehaviorCase BuildExternalAssemblyByTypeCase(
        string snapshotName,
        DeclarationType declarationType,
        AccessType accessType)
    {
        var fixtureName = $"ByType{declarationType}{accessType}";

        var fixture = NameofGeneratorBehaviorAssemblyFactory.CreateExternalFixture(
            assemblyName: $"Behavior.External.ByType.{declarationType}.{accessType}",
            declarationType,
            accessType,
            typeName: fixtureName);

        var source =
            $$"""
            using Nameof;

            [assembly: GenerateNameof<ExternalFixtures.{{fixtureName}}>]
            """;

        return CreateBehaviorCase(snapshotName, source, fixture.Reference);
    }

    private static BehaviorCase BuildExternalAssemblyByAssemblyNameCase(
        string snapshotName,
        DeclarationType declarationType,
        AccessType accessType)
    {
        var fixtureName = $"ByAssemblyName{declarationType}{accessType}";
        var assemblyName = $"Behavior.External.ByAssemblyName.{declarationType}.{accessType}";

        var fixture = NameofGeneratorBehaviorAssemblyFactory.CreateExternalFixture(
            assemblyName,
            declarationType,
            accessType,
            typeName: fixtureName);

        var decoy = NameofGeneratorBehaviorAssemblyFactory.CreateDecoyFixture(
            assemblyName: $"{assemblyName}.Decoy",
            declarationType,
            typeName: fixtureName);

        var source =
            $$"""
            using Nameof;

            [assembly: GenerateNameof("ExternalFixtures.{{fixtureName}}", assemblyName: "{{assemblyName}}")]
            """;

        return CreateBehaviorCase(snapshotName, source, fixture.Reference, decoy);
    }

    private static BehaviorCase BuildExternalAssemblyByAssemblyOfCase(
        string snapshotName,
        DeclarationType declarationType,
        AccessType accessType)
    {
        var fixtureName = $"ByAssemblyOf{declarationType}{accessType}";
        var anchorName = $"ByAssemblyOf{declarationType}{accessType}Anchor";
        var assemblyName = $"Behavior.External.ByAssemblyOf.{declarationType}.{accessType}";

        var fixture = NameofGeneratorBehaviorAssemblyFactory.CreateExternalFixture(
            assemblyName,
            declarationType,
            accessType,
            typeName: fixtureName,
            includeAnchor: true,
            anchorTypeName: anchorName);

        var decoy = NameofGeneratorBehaviorAssemblyFactory.CreateDecoyFixture(
            assemblyName: $"{assemblyName}.Decoy",
            declarationType,
            typeName: fixtureName);

        var source =
            $$"""
            using Nameof;

            [assembly: GenerateNameof("ExternalFixtures.{{fixtureName}}", assemblyOf: typeof(ExternalFixtures.{{anchorName}}))]
            """;

        return CreateBehaviorCase(snapshotName, source, fixture.Reference, decoy);
    }

    private static BehaviorCase CreateBehaviorCase(
        string snapshotName,
        string source,
        params MetadataReference[] references)
    {
        return new BehaviorCase(snapshotName, source, references);
    }
}
