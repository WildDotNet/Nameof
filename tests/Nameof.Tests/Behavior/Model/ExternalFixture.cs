using Microsoft.CodeAnalysis;

namespace Nameof.Tests.Behavior.Model;

internal sealed record ExternalFixture(
    MetadataReference Reference,
    string TypeName,
    string AssemblyName);
