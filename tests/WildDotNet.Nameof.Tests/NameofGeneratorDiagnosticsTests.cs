using System.Collections.Generic;
using WildDotNet.Nameof.Tests.TestInfrastructure;

namespace WildDotNet.Nameof.Tests;

public class NameofGeneratorDiagnosticsTests
{
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
