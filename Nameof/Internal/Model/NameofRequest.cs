using Microsoft.CodeAnalysis;

namespace Nameof.Internal.Model;

internal readonly record struct NameofRequest(
    INamedTypeSymbol? Symbol,
    string? FullTypeName,
    INamedTypeSymbol? AssemblyOfType,
    string? AssemblyName);
