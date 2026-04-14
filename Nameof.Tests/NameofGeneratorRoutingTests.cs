using Nameof.Tests.TestInfrastructure;

namespace Nameof.Tests;

public class NameofGeneratorRoutingTests
{
    [Fact]
    public Task Current_assembly_full_type_name_request_uses_source_resolution()
    {
        const string source =
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
}
