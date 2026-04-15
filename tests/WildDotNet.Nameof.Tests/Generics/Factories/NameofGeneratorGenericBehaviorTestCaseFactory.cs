using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Tests.Generics.Model;

namespace WildDotNet.Nameof.Tests.Generics.Factories;

internal static class NameofGeneratorGenericBehaviorTestCaseFactory
{
    internal static GenericBehaviorScenarioCase BuildGenericScenarioCase(
        AssemblyType assemblyType,
        GenericDeclarationType declarationType,
        Arity arity,
        AccessType accessType)
    {
        var snapshotName = $"{assemblyType}_{declarationType}_{arity}_{accessType}";

        return assemblyType switch
        {
            AssemblyType.CurrentAssembly => CreateScenarioCase(
                snapshotName,
                lookupKind => BuildGenericBehaviorCase(assemblyType, lookupKind, declarationType, arity, accessType)),

            AssemblyType.ExternalAssembly => BuildExternalAssemblyScenarioCase(snapshotName, declarationType, arity, accessType),

            _ => throw new ArgumentOutOfRangeException(nameof(assemblyType)),
        };
    }

    internal static GenericBehaviorScenarioCase BuildCoexistenceScenarioCase(
        AssemblyType assemblyType,
        AccessType nonGenericAccessType,
        AccessType genericAccessType)
    {
        var snapshotName = $"{assemblyType}_SharedName_{nonGenericAccessType}_{genericAccessType}";

        return assemblyType switch
        {
            AssemblyType.CurrentAssembly => CreateScenarioCase(
                snapshotName,
                lookupKind => BuildCoexistenceBehaviorCase(assemblyType, lookupKind, nonGenericAccessType, genericAccessType)),

            AssemblyType.ExternalAssembly => BuildExternalCoexistenceScenarioCase(snapshotName, nonGenericAccessType, genericAccessType),

            _ => throw new ArgumentOutOfRangeException(nameof(assemblyType)),
        };
    }

    private static GenericBehaviorScenarioCase BuildExternalAssemblyScenarioCase(
        string snapshotName,
        GenericDeclarationType declarationType,
        Arity arity,
        AccessType accessType)
    {
        var typeName = $"{declarationType}{accessType}{arity}";
        var anchorName = $"{typeName}Anchor";
        var assemblyName = $"Generic.External.{declarationType}.{arity}.{accessType}";

        var fixture = NameofGeneratorGenericBehaviorAssemblyFactory.CreateExternalFixture(
            assemblyName,
            declarationType,
            accessType,
            arity,
            typeName,
            includeAnchor: true,
            anchorTypeName: anchorName);

        var decoy = NameofGeneratorGenericBehaviorAssemblyFactory.CreateDecoyFixture(
            assemblyName: $"{assemblyName}.Decoy",
            declarationType,
            accessType,
            arity,
            typeName);

        var metadataName = GenericArityUtilities.GetMetadataName($"GenericFixtures.{fixture.TypeName}", arity);

        var byTypeSource =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof(typeof(GenericFixtures.{{fixture.TypeName}}{{GenericArityUtilities.GetOpenGenericTypeArguments(arity)}}))]
            """;

        var byAssemblyNameSource =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("{{metadataName}}", assemblyName: "{{assemblyName}}")]
            """;

        var byAssemblyOfSource =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("{{metadataName}}", assemblyOf: typeof(GenericFixtures.{{anchorName}}))]
            """;

        return CreateScenarioCase(
            snapshotName,
            lookupKind => lookupKind switch
            {
                LookupKind.ByType => CreateBehaviorCase($"{snapshotName}_ByType", byTypeSource, fixture.Reference),
                LookupKind.ByAssemblyName => CreateBehaviorCase($"{snapshotName}_ByAssemblyName", byAssemblyNameSource, fixture.Reference, decoy),
                LookupKind.ByAssemblyOf => CreateBehaviorCase($"{snapshotName}_ByAssemblyOf", byAssemblyOfSource, fixture.Reference, decoy),
                _ => throw new ArgumentOutOfRangeException(nameof(lookupKind)),
            });
    }

    private static GenericBehaviorScenarioCase BuildExternalCoexistenceScenarioCase(
        string snapshotName,
        AccessType nonGenericAccessType,
        AccessType genericAccessType)
    {
        const string typeName = "SharedName";
        const string anchorName = "SharedNameAnchor";
        var assemblyName = $"Generic.External.Coexistence.{nonGenericAccessType}.{genericAccessType}";

        var fixture = NameofGeneratorGenericBehaviorAssemblyFactory.CreateCoexistenceFixture(
            assemblyName,
            nonGenericAccessType,
            genericAccessType,
            typeName,
            includeAnchor: true,
            anchorTypeName: anchorName);

        var decoy = NameofGeneratorGenericBehaviorAssemblyFactory.CreateCoexistenceDecoyFixture(
            assemblyName: $"{assemblyName}.Decoy",
            nonGenericAccessType,
            genericAccessType,
            typeName);

        var byTypeSource =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof(typeof(GenericFixtures.SharedName<>))]
            """;

        var byAssemblyNameSource =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("GenericFixtures.SharedName`1", assemblyName: "{{assemblyName}}")]
            """;

        var byAssemblyOfSource =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("GenericFixtures.SharedName`1", assemblyOf: typeof(GenericFixtures.{{anchorName}}))]
            """;

        return CreateScenarioCase(
            snapshotName,
            lookupKind => lookupKind switch
            {
                LookupKind.ByType => CreateBehaviorCase($"{snapshotName}_ByType", byTypeSource, fixture.Reference),
                LookupKind.ByAssemblyName => CreateBehaviorCase($"{snapshotName}_ByAssemblyName", byAssemblyNameSource, fixture.Reference, decoy),
                LookupKind.ByAssemblyOf => CreateBehaviorCase($"{snapshotName}_ByAssemblyOf", byAssemblyOfSource, fixture.Reference, decoy),
                _ => throw new ArgumentOutOfRangeException(nameof(lookupKind)),
            });
    }

    private static GenericBehaviorCase BuildGenericBehaviorCase(
        AssemblyType assemblyType,
        LookupKind lookupKind,
        GenericDeclarationType declarationType,
        Arity arity,
        AccessType accessType)
    {
        return (assemblyType, lookupKind) switch
        {
            (AssemblyType.CurrentAssembly, _)
                => CreateBehaviorCase(
                    $"{assemblyType}_{lookupKind}_{declarationType}_{arity}_{accessType}",
                    NameofGeneratorGenericBehaviorSourceFactory.CreateCurrentAssemblySource(
                        lookupKind,
                        declarationType,
                        arity,
                        accessType)),

            (AssemblyType.ExternalAssembly, LookupKind.ByType)
                => BuildExternalAssemblyByTypeCase($"{assemblyType}_{lookupKind}_{declarationType}_{arity}_{accessType}", declarationType, arity, accessType),

            (AssemblyType.ExternalAssembly, LookupKind.ByAssemblyName)
                => BuildExternalAssemblyByAssemblyNameCase($"{assemblyType}_{lookupKind}_{declarationType}_{arity}_{accessType}", declarationType, arity, accessType),

            (AssemblyType.ExternalAssembly, LookupKind.ByAssemblyOf)
                => BuildExternalAssemblyByAssemblyOfCase($"{assemblyType}_{lookupKind}_{declarationType}_{arity}_{accessType}", declarationType, arity, accessType),

            _ => throw new ArgumentOutOfRangeException(nameof(lookupKind)),
        };
    }

    private static GenericBehaviorCase BuildCoexistenceBehaviorCase(
        AssemblyType assemblyType,
        LookupKind lookupKind,
        AccessType nonGenericAccessType,
        AccessType genericAccessType)
    {
        return (assemblyType, lookupKind) switch
        {
            (AssemblyType.CurrentAssembly, _)
                => CreateBehaviorCase(
                    $"{assemblyType}_{lookupKind}_SharedName_{nonGenericAccessType}_{genericAccessType}",
                    NameofGeneratorGenericBehaviorSourceFactory.CreateCurrentAssemblyCoexistenceSource(
                        lookupKind,
                        nonGenericAccessType,
                        genericAccessType)),

            (AssemblyType.ExternalAssembly, LookupKind.ByType)
                => BuildExternalCoexistenceByTypeCase($"{assemblyType}_{lookupKind}_SharedName_{nonGenericAccessType}_{genericAccessType}", nonGenericAccessType, genericAccessType),

            (AssemblyType.ExternalAssembly, LookupKind.ByAssemblyName)
                => BuildExternalCoexistenceByAssemblyNameCase($"{assemblyType}_{lookupKind}_SharedName_{nonGenericAccessType}_{genericAccessType}", nonGenericAccessType, genericAccessType),

            (AssemblyType.ExternalAssembly, LookupKind.ByAssemblyOf)
                => BuildExternalCoexistenceByAssemblyOfCase($"{assemblyType}_{lookupKind}_SharedName_{nonGenericAccessType}_{genericAccessType}", nonGenericAccessType, genericAccessType),

            _ => throw new ArgumentOutOfRangeException(nameof(lookupKind)),
        };
    }

    private static GenericBehaviorCase BuildExternalAssemblyByTypeCase(
        string snapshotName,
        GenericDeclarationType declarationType,
        Arity arity,
        AccessType accessType)
    {
        var fixtureName = $"{declarationType}{accessType}{arity}";

        var fixture = NameofGeneratorGenericBehaviorAssemblyFactory.CreateExternalFixture(
            assemblyName: $"Generic.External.ByType.{declarationType}.{arity}.{accessType}",
            declarationType,
            accessType,
            arity,
            typeName: fixtureName);

        var source =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof(typeof(GenericFixtures.{{fixtureName}}{{GenericArityUtilities.GetOpenGenericTypeArguments(arity)}}))]
            """;

        return CreateBehaviorCase(snapshotName, source, fixture.Reference);
    }

    private static GenericBehaviorCase BuildExternalAssemblyByAssemblyNameCase(
        string snapshotName,
        GenericDeclarationType declarationType,
        Arity arity,
        AccessType accessType)
    {
        var fixtureName = $"{declarationType}{accessType}{arity}";
        var assemblyName = $"Generic.External.ByAssemblyName.{declarationType}.{arity}.{accessType}";

        var fixture = NameofGeneratorGenericBehaviorAssemblyFactory.CreateExternalFixture(
            assemblyName,
            declarationType,
            accessType,
            arity,
            typeName: fixtureName);

        var decoy = NameofGeneratorGenericBehaviorAssemblyFactory.CreateDecoyFixture(
            assemblyName: $"{assemblyName}.Decoy",
            declarationType,
            accessType,
            arity,
            typeName: fixtureName);

        var metadataName = GenericArityUtilities.GetMetadataName($"GenericFixtures.{fixtureName}", arity);

        var source =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("{{metadataName}}", assemblyName: "{{assemblyName}}")]
            """;

        return CreateBehaviorCase(snapshotName, source, fixture.Reference, decoy);
    }

    private static GenericBehaviorCase BuildExternalAssemblyByAssemblyOfCase(
        string snapshotName,
        GenericDeclarationType declarationType,
        Arity arity,
        AccessType accessType)
    {
        var fixtureName = $"{declarationType}{accessType}{arity}";
        var anchorName = $"{fixtureName}Anchor";
        var assemblyName = $"Generic.External.ByAssemblyOf.{declarationType}.{arity}.{accessType}";

        var fixture = NameofGeneratorGenericBehaviorAssemblyFactory.CreateExternalFixture(
            assemblyName,
            declarationType,
            accessType,
            arity,
            typeName: fixtureName,
            includeAnchor: true,
            anchorTypeName: anchorName);

        var decoy = NameofGeneratorGenericBehaviorAssemblyFactory.CreateDecoyFixture(
            assemblyName: $"{assemblyName}.Decoy",
            declarationType,
            accessType,
            arity,
            typeName: fixtureName);

        var metadataName = GenericArityUtilities.GetMetadataName($"GenericFixtures.{fixtureName}", arity);

        var source =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("{{metadataName}}", assemblyOf: typeof(GenericFixtures.{{anchorName}}))]
            """;

        return CreateBehaviorCase(snapshotName, source, fixture.Reference, decoy);
    }

    private static GenericBehaviorCase BuildExternalCoexistenceByTypeCase(
        string snapshotName,
        AccessType nonGenericAccessType,
        AccessType genericAccessType)
    {
        const string typeName = "SharedName";

        var fixture = NameofGeneratorGenericBehaviorAssemblyFactory.CreateCoexistenceFixture(
            assemblyName: $"Generic.External.Coexistence.ByType.{nonGenericAccessType}.{genericAccessType}",
            nonGenericAccessType,
            genericAccessType,
            typeName);

        var source =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof(typeof(GenericFixtures.SharedName<>))]
            """;

        return CreateBehaviorCase(snapshotName, source, fixture.Reference);
    }

    private static GenericBehaviorCase BuildExternalCoexistenceByAssemblyNameCase(
        string snapshotName,
        AccessType nonGenericAccessType,
        AccessType genericAccessType)
    {
        const string typeName = "SharedName";
        var assemblyName = $"Generic.External.Coexistence.ByAssemblyName.{nonGenericAccessType}.{genericAccessType}";

        var fixture = NameofGeneratorGenericBehaviorAssemblyFactory.CreateCoexistenceFixture(
            assemblyName,
            nonGenericAccessType,
            genericAccessType,
            typeName);

        var decoy = NameofGeneratorGenericBehaviorAssemblyFactory.CreateCoexistenceDecoyFixture(
            assemblyName: $"{assemblyName}.Decoy",
            nonGenericAccessType,
            genericAccessType,
            typeName);

        var source =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("GenericFixtures.SharedName`1", assemblyName: "{{assemblyName}}")]
            """;

        return CreateBehaviorCase(snapshotName, source, fixture.Reference, decoy);
    }

    private static GenericBehaviorCase BuildExternalCoexistenceByAssemblyOfCase(
        string snapshotName,
        AccessType nonGenericAccessType,
        AccessType genericAccessType)
    {
        const string typeName = "SharedName";
        const string anchorName = "SharedNameAnchor";
        var assemblyName = $"Generic.External.Coexistence.ByAssemblyOf.{nonGenericAccessType}.{genericAccessType}";

        var fixture = NameofGeneratorGenericBehaviorAssemblyFactory.CreateCoexistenceFixture(
            assemblyName,
            nonGenericAccessType,
            genericAccessType,
            typeName,
            includeAnchor: true,
            anchorTypeName: anchorName);

        var decoy = NameofGeneratorGenericBehaviorAssemblyFactory.CreateCoexistenceDecoyFixture(
            assemblyName: $"{assemblyName}.Decoy",
            nonGenericAccessType,
            genericAccessType,
            typeName);

        var source =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("GenericFixtures.SharedName`1", assemblyOf: typeof(GenericFixtures.{{anchorName}}))]
            """;

        return CreateBehaviorCase(snapshotName, source, fixture.Reference, decoy);
    }

    private static GenericBehaviorScenarioCase CreateScenarioCase(
        string snapshotName,
        Func<LookupKind, GenericBehaviorCase> lookupCaseFactory)
    {
        return new GenericBehaviorScenarioCase(
            snapshotName,
            lookupCaseFactory(LookupKind.ByType),
            lookupCaseFactory(LookupKind.ByAssemblyName),
            lookupCaseFactory(LookupKind.ByAssemblyOf));
    }

    private static GenericBehaviorCase CreateBehaviorCase(
        string snapshotName,
        string source,
        params MetadataReference[] references)
    {
        return new GenericBehaviorCase(snapshotName, source, references);
    }
}
