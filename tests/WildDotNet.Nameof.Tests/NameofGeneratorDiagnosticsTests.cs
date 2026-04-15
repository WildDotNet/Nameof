using System.Collections.Generic;
using WildDotNet.Nameof.Tests.TestInfrastructure;

namespace WildDotNet.Nameof.Tests;

public class NameofGeneratorDiagnosticsTests
{
    public enum DuplicateScenario
    {
        TypeAndType,
        TypeAndAssemblyName,
        TypeAndAssemblyOf,
        FullTypeNameAndAssemblyName,
        FullTypeNameAndAssemblyOf
    }

    [Fact]
    public Task Reports_warning_for_unsupported_full_type_name()
    {
        const string source =
            """
            using WildDotNet.Nameof;
            [assembly: GenerateNameof("SomeNamespace.Outer+Inner", assemblyName: "Anything")]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Fact]
    public Task Reports_warning_for_closed_generic_type_using_typeof()
    {
        const string source =
            """
            using System.Collections.Generic;
            using WildDotNet.Nameof;

            [assembly: GenerateNameof(typeof(List<int>))]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Fact]
    public Task Reports_warning_for_closed_generic_type_using_assembly_name()
    {
        const string source =
            """
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("System.Collections.Generic.List[System.Int32]", assemblyName: "System.Private.CoreLib")]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Fact]
    public Task Does_not_report_warning_for_open_generic_full_type_name()
    {
        const string source =
            """
            using WildDotNet.Nameof;

            [assembly: GenerateNameof("System.Collections.Generic.List`1", assemblyName: "System.Private.CoreLib")]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Theory]
    [CombinatorialData]
    public Task Reports_warning_for_duplicate_request(DuplicateScenario scenario)
    {
        var source = scenario switch
        {
            DuplicateScenario.TypeAndType =>
                """
                using System;
                using WildDotNet.Nameof;

                [assembly: GenerateNameof(typeof(ConsoleKeyInfo))]
                [assembly: GenerateNameof(typeof(ConsoleKeyInfo))]
                """,

            DuplicateScenario.TypeAndAssemblyName =>
                """
                using System;
                using WildDotNet.Nameof;

                [assembly: GenerateNameof(typeof(ConsoleKeyInfo))]
                [assembly: GenerateNameof("System.ConsoleKeyInfo", assemblyName: "System.Console")]
                """,

            DuplicateScenario.TypeAndAssemblyOf =>
                """
                using System;
                using WildDotNet.Nameof;

                [assembly: GenerateNameof(typeof(ConsoleKeyInfo))]
                [assembly: GenerateNameof("System.ConsoleKeyInfo", assemblyOf: typeof(Console))]
                """,

            DuplicateScenario.FullTypeNameAndAssemblyName =>
                """
                using WildDotNet.Nameof;

                [assembly: GenerateNameof("System.Collections.Generic.List`1", assemblyName: "System.Private.CoreLib")]
                [assembly: GenerateNameof("System.Collections.Generic.List`1", assemblyName: "System.Private.CoreLib")]
                """,

            DuplicateScenario.FullTypeNameAndAssemblyOf =>
                """
                using System.Collections.Generic;
                using WildDotNet.Nameof;

                [assembly: GenerateNameof("System.Collections.Generic.List`1", assemblyOf: typeof(Dictionary<,>))]
                [assembly: GenerateNameof("System.Collections.Generic.List`1", assemblyOf: typeof(Dictionary<,>))]
                """,

            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot()).UseTextForParameters(scenario.ToString());
    }

    [Fact]
    public Task Reports_warning_for_unresolved_external_type_using_assembly_of()
    {
        const string source =
            """
            using System;
            using WildDotNet.Nameof;
            [assembly: GenerateNameof("System.NotARealType", assemblyOf: typeof(Console))]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Fact]
    public Task Reports_warning_for_unresolved_external_type_using_assembly_name()
    {
        const string source =
            """
            using WildDotNet.Nameof;
            [assembly: GenerateNameof("System.NotARealType", assemblyName: "System.Console")]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }
}
