using Nameof.Tests.TestInfrastructure;

namespace Nameof.Tests;

public class NameofGeneratorDiagnosticsTests
{
    [Fact]
    public Task Reports_warning_for_unsupported_full_type_name()
    {
        const string source =
            """
            using Nameof;
            [assembly: GenerateNameof("SomeNamespace.SomeType`1", assemblyName: "Anything")]
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
            using Nameof;
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
            using Nameof;
            [assembly: GenerateNameof("System.NotARealType", assemblyName: "System.Console")]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }
}
