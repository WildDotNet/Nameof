using Nameof.Tests.TestInfrastructure;

namespace Nameof.Tests;

public class NameofGeneratorBehaviorTests
{
    [Fact]
    public Task Generates_members_for_current_assembly_internal_type()
    {
        const string source =
            """
            using Nameof;
            [assembly: GenerateNameof(typeof(SomeType))]

            internal class SomeType
            {
                private int _someField;
                private void SomeMethod() { }
                private string SomeProperty { get; set; } = "";
            }
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Fact]
    public Task Generates_members_for_external_public_type_private_fields()
    {
        const string source =
            """
            using System;
            using Nameof;
            [assembly: GenerateNameof<ConsoleKeyInfo>]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Fact]
    public Task Generates_stub_and_members_for_external_non_public_type_using_assembly_of()
    {
        const string source =
            """
            using System;
            using Nameof;
            [assembly: GenerateNameof("System.IO.ConsoleStream", assemblyOf: typeof(Console))]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }

    [Fact]
    public Task Generates_members_for_external_non_public_type_using_assembly_name()
    {
        const string source =
            """
            using Nameof;
            [assembly: GenerateNameof("Nameof.NameofGenerator", assemblyName: "Nameof")]
            """;

        var result = GeneratorTestDriver.Run(source);

        return Verify(result.ToSnapshot());
    }
}
