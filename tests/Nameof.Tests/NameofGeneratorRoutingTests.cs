using Nameof.Tests.TestInfrastructure;

namespace Nameof.Tests;

public class NameofGeneratorRoutingTests
{
    [Fact]
    public Task Current_assembly_full_type_name_request_using_assembly_name_uses_source_resolution()
    {
        var source =
            """
            using Nameof;

            [assembly: GenerateNameof("LocalNamespace.SomeType", assemblyName: "GeneratorTests")]

            namespace LocalNamespace;

            internal class SomeType
            {
                private int _value;
            }
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Fact]
    public Task Current_assembly_full_type_name_request_using_assembly_of_uses_source_resolution()
    {
        var source =
            """
            using Nameof;

            [assembly: GenerateNameof("LocalNamespace.SomeType", assemblyOf: typeof(LocalNamespace.AssemblyAnchor))]

            namespace LocalNamespace;

            public sealed class AssemblyAnchor;

            internal class SomeType
            {
                private int _value;
            }
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }
}
