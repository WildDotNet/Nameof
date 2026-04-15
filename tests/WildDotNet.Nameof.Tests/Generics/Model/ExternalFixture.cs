using Microsoft.CodeAnalysis;

namespace WildDotNet.Nameof.Tests.Generics.Model;

internal sealed record ExternalFixture(
    MetadataReference Reference,
    string TypeName,
    string AssemblyName);
