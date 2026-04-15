using WildDotNet.Nameof.Tests.Behavior.Factories;
using WildDotNet.Nameof.Tests.Behavior.Model;
using WildDotNet.Nameof.Tests.TestInfrastructure;

namespace WildDotNet.Nameof.Tests.Behavior;

public class NameofGeneratorBehaviorTests
{
    [Theory]
    [CombinatorialData]
    public Task Generates_nameof_behavior(
        AssemblyType assemblyType,
        DeclarationType declarationType,
        AccessType accessType)
    {
        var testCase = NameofGeneratorBehaviorTestCaseFactory.BuildBehaviorScenarioCase(
            assemblyType,
            declarationType,
            accessType);

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
        Assert.Equal(expected.GeneratedSources, actual.GeneratedSources);
    }
}
