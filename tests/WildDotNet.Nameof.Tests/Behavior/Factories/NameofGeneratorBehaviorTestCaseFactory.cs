using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Tests.Behavior.Model;

namespace WildDotNet.Nameof.Tests.Behavior.Factories;

internal static class NameofGeneratorBehaviorTestCaseFactory
{
    internal static BehaviorScenarioCase BuildBehaviorScenarioCase(
        AssemblyType assemblyType,
        DeclarationType declarationType,
        AccessType accessType)
    {
        var snapshotName = $"{assemblyType}_{declarationType}_{accessType}";

        return assemblyType switch
        {
            AssemblyType.CurrentAssembly => new BehaviorScenarioCase(
                snapshotName,
                BuildBehaviorCase(assemblyType, LookupKind.ByType, declarationType, accessType),
                BuildBehaviorCase(assemblyType, LookupKind.ByAssemblyName, declarationType, accessType),
                BuildBehaviorCase(assemblyType, LookupKind.ByAssemblyOf, declarationType, accessType)),

            AssemblyType.ExternalAssembly => BuildExternalAssemblyScenarioCase(snapshotName, declarationType, accessType),

            _ => throw new ArgumentOutOfRangeException(nameof(assemblyType))
        };
    }

    private static BehaviorCase BuildBehaviorCase(
        AssemblyType assemblyType,
        LookupKind lookupKind,
        DeclarationType declarationType,
        AccessType accessType)
    {
        return (assemblyType, lookupKind) switch
        {
            (AssemblyType.CurrentAssembly, _)
                => CreateBehaviorCase(
                    $"{assemblyType}_{lookupKind}_{declarationType}_{accessType}",
                    NameofGeneratorBehaviorSourceFactory.CreateCurrentAssemblySource(
                        lookupKind,
                        declarationType,
                        accessType)),

            (AssemblyType.ExternalAssembly, LookupKind.ByType)
                => BuildExternalAssemblyByTypeCase($"{assemblyType}_{lookupKind}_{declarationType}_{accessType}", declarationType, accessType),

            (AssemblyType.ExternalAssembly, LookupKind.ByAssemblyName)
                => BuildExternalAssemblyByAssemblyNameCase($"{assemblyType}_{lookupKind}_{declarationType}_{accessType}", declarationType, accessType),

            (AssemblyType.ExternalAssembly, LookupKind.ByAssemblyOf)
                => BuildExternalAssemblyByAssemblyOfCase($"{assemblyType}_{lookupKind}_{declarationType}_{accessType}", declarationType, accessType),

            _ => throw new ArgumentOutOfRangeException(nameof(lookupKind)),
        };
    }

    private static BehaviorScenarioCase BuildExternalAssemblyScenarioCase(
        string snapshotName,
        DeclarationType declarationType,
        AccessType accessType)
    {
        var typeName = $"{declarationType}{accessType}";
        var anchorName = $"{typeName}Anchor";
        var assemblyName = $"Behavior.External.{declarationType}.{accessType}";

        var fixture = NameofGeneratorBehaviorAssemblyFactory.CreateExternalFixture(
            assemblyName,
            declarationType,
            accessType,
            typeName,
            includeAnchor: true,
            anchorTypeName: anchorName);

        var decoy = NameofGeneratorBehaviorAssemblyFactory.CreateDecoyFixture(
            assemblyName: $"{assemblyName}.Decoy",
            declarationType,
            typeName);

        var byTypeSource =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof(typeof(ExternalFixtures.{{typeName}}))]
            """;

        var byAssemblyNameSource =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("ExternalFixtures.{{typeName}}", assemblyName: "{{assemblyName}}")]
            """;

        var byAssemblyOfSource =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("ExternalFixtures.{{typeName}}", assemblyOf: typeof(ExternalFixtures.{{anchorName}}))]
            """;

        return new BehaviorScenarioCase(
            snapshotName,
            CreateBehaviorCase($"{snapshotName}_ByType", byTypeSource, fixture.Reference),
            CreateBehaviorCase($"{snapshotName}_ByAssemblyName", byAssemblyNameSource, fixture.Reference, decoy),
            CreateBehaviorCase($"{snapshotName}_ByAssemblyOf", byAssemblyOfSource, fixture.Reference, decoy));
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
            using WildDotNet.Nameof;

            [assembly: GenerateNameof(typeof(ExternalFixtures.{{fixtureName}}))]
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
            using WildDotNet.Nameof;

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
            using WildDotNet.Nameof;

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
