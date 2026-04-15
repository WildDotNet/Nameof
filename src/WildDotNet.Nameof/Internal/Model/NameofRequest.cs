using Microsoft.CodeAnalysis;

namespace WildDotNet.Nameof.Internal.Model;

internal readonly record struct NameofRequest(
    INamedTypeSymbol? Symbol,
    string? FullTypeName,
    INamedTypeSymbol? AssemblyOfType,
    string? AssemblyName,
    Location? AttributeLocation,
    bool IsOpenGenericDefinition = false,
    bool IsClosedGeneric = false,
    int GenericArity = 0);
