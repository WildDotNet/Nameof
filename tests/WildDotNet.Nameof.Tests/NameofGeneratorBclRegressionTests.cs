using System.Collections.Generic;
using WildDotNet.Nameof.Tests.TestInfrastructure;

namespace WildDotNet.Nameof.Tests;

public class NameofGeneratorBclRegressionTests
{
    [Fact]
    public Task Generates_nameof_for_bcl_public_type_using_typeof()
    {
        const string source =
            """
            using System;
            using WildDotNet.Nameof;

            [assembly: GenerateNameof(typeof(ConsoleKeyInfo))]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Fact]
    public Task Generates_nameof_for_bcl_internal_type_using_assembly_of()
    {
        const string source =
            """
            using System;
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("System.IO.ConsoleStream", assemblyOf: typeof(Console))]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Fact]
    public Task Generates_nameof_for_bcl_open_generic_list_using_lookup_kind_equivalence()
    {
        return RunOpenGenericRegressionScenario(
            nameof(Generates_nameof_for_bcl_open_generic_list_using_lookup_kind_equivalence),
            "typeof(List<>)",
            "System.Collections.Generic.List`1");
    }

    [Fact]
    public Task Generates_nameof_for_bcl_open_generic_dictionary_using_lookup_kind_equivalence()
    {
        return RunOpenGenericRegressionScenario(
            nameof(Generates_nameof_for_bcl_open_generic_dictionary_using_lookup_kind_equivalence),
            "typeof(Dictionary<,>)",
            "System.Collections.Generic.Dictionary`2");
    }

    private static Task RunOpenGenericRegressionScenario(string snapshotName, string typeExpression, string metadataName)
    {
        var assemblyName = typeof(List<>).Assembly.GetName().Name!;

        var byTypeSource =
            $$"""
            using System.Collections.Generic;
            using WildDotNet.Nameof;

            [assembly: GenerateNameof({{typeExpression}})]
            """;

        var byAssemblyNameSource =
            $$"""
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("{{metadataName}}", assemblyName: "{{assemblyName}}")]
            """;

        var byAssemblyOfSource =
            $$"""
            using System.Collections.Generic;
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("{{metadataName}}", assemblyOf: typeof(Dictionary<,>))]
            """;

        var byType = GeneratorTestDriver.Run(byTypeSource).ToSnapshot();
        var byAssemblyName = GeneratorTestDriver.Run(byAssemblyNameSource).ToSnapshot();
        var byAssemblyOf = GeneratorTestDriver.Run(byAssemblyOfSource).ToSnapshot();

        AssertEquivalent(byType, byAssemblyName);
        AssertEquivalent(byType, byAssemblyOf);

        return Verify(byType).UseTextForParameters(snapshotName);
    }

    private static void AssertEquivalent(GeneratorRunSnapshot expected, GeneratorRunSnapshot actual)
    {
        Assert.Equal(expected.Diagnostics, actual.Diagnostics);
        Assert.Equal(expected.GeneratedSources.Select(static source => source.HintName), actual.GeneratedSources.Select(static source => source.HintName));
        Assert.Equal(expected.GeneratedSources, actual.GeneratedSources);
    }
}
