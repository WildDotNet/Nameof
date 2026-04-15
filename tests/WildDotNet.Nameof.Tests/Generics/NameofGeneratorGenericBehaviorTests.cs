using WildDotNet.Nameof.Tests.Generics.Factories;
using WildDotNet.Nameof.Tests.Generics.Model;
using WildDotNet.Nameof.Tests.TestInfrastructure;

namespace WildDotNet.Nameof.Tests.Generics;

public class NameofGeneratorGenericBehaviorTests
{
    [Theory]
    [CombinatorialData]
    public Task Generates_nameof_generic_behavior(
        AssemblyType assemblyType,
        GenericDeclarationType declarationType,
        Arity arity,
        AccessType accessType)
    {
        var testCase = NameofGeneratorGenericBehaviorTestCaseFactory.BuildGenericScenarioCase(
            assemblyType,
            declarationType,
            arity,
            accessType);

        var byType = GeneratorTestDriver.Run(testCase.ByType.Source, testCase.ByType.References).ToSnapshot();
        var byAssemblyName = GeneratorTestDriver.Run(testCase.ByAssemblyName.Source, testCase.ByAssemblyName.References).ToSnapshot();
        var byAssemblyOf = GeneratorTestDriver.Run(testCase.ByAssemblyOf.Source, testCase.ByAssemblyOf.References).ToSnapshot();

        AssertEquivalent(byType, byAssemblyName);
        AssertEquivalent(byType, byAssemblyOf);

        return Verify(byType).UseTextForParameters(testCase.SnapshotName);
    }

    [Theory]
    [CombinatorialData]
    public Task Generates_nameof_generic_coexistence(
        AssemblyType assemblyType,
        AccessType nonGenericAccessType,
        AccessType genericAccessType)
    {
        var testCase = NameofGeneratorGenericBehaviorTestCaseFactory.BuildCoexistenceScenarioCase(
            assemblyType,
            nonGenericAccessType,
            genericAccessType);

        var byType = GeneratorTestDriver.Run(testCase.ByType.Source, testCase.ByType.References).ToSnapshot();
        var byAssemblyName = GeneratorTestDriver.Run(testCase.ByAssemblyName.Source, testCase.ByAssemblyName.References).ToSnapshot();
        var byAssemblyOf = GeneratorTestDriver.Run(testCase.ByAssemblyOf.Source, testCase.ByAssemblyOf.References).ToSnapshot();

        AssertEquivalent(byType, byAssemblyName);
        AssertEquivalent(byType, byAssemblyOf);

        return Verify(byType).UseTextForParameters(testCase.SnapshotName);
    }

    private static void AssertEquivalent(GeneratorRunSnapshot expected, GeneratorRunSnapshot actual)
    {
        Assert.Equal(expected.Diagnostics, actual.Diagnostics);
        Assert.Equal(expected.GeneratedSources.Select(static source => source.HintName), actual.GeneratedSources.Select(static source => source.HintName));
        Assert.Equal(expected.GeneratedSources, actual.GeneratedSources);
    }
}
