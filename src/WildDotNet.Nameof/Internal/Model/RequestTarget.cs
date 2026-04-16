using System;
using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof.Internal.Model;

internal readonly record struct RequestTarget
{
    public INamedTypeSymbol? Symbol { get; }

    public string? FullTypeName { get; }

    public INamedTypeSymbol? AssemblyOfType { get; }

    public string? AssemblyName { get; }

    public bool IsSymbolTarget => Symbol is not null;

    public bool IsFullNameTarget => FullTypeName is not null;

    public bool HasAssemblyName => AssemblyName is not null;

    public string RequestedAssemblyName =>
        Symbol?.ContainingAssembly.Identity.Name ??
        AssemblyOfType?.ContainingAssembly.Identity.Name ??
        AssemblyName ??
        string.Empty;

    public string DiagnosticDisplayName =>
        Symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ??
        FullTypeName ??
        string.Empty;

    public string DuplicateAssemblyTargetDisplayName =>
        Symbol?.ContainingAssembly.Identity.Name ??
        AssemblyOfType?.ContainingAssembly.Identity.Name ??
        AssemblyName ??
        string.Empty;

    public string DeduplicationKey =>
        Symbol is not null
            ? $"{Symbol.ContainingAssembly.Identity.Name}|{TypeNameUtilities.GetMetadataFullName(Symbol)}"
            : $"{DuplicateAssemblyTargetDisplayName}|{FullTypeName}";

    private RequestTarget(
        INamedTypeSymbol? symbol,
        string? fullTypeName,
        INamedTypeSymbol? assemblyOfType,
        string? assemblyName)
    {
        if (symbol is not null)
        {
            if (fullTypeName is not null || assemblyOfType is not null || assemblyName is not null)
            {
                throw new ArgumentException("Symbol targets cannot carry full type or assembly data.", nameof(symbol));
            }

            Symbol = symbol;
            FullTypeName = null;
            AssemblyOfType = null;
            AssemblyName = null;
            return;
        }

        if (fullTypeName is null)
        {
            throw new ArgumentException("Full type name is required for non-symbol targets.", nameof(fullTypeName));
        }

        if (assemblyOfType is not null && assemblyName is not null)
        {
            throw new ArgumentException("A full-name target can reference either an assembly symbol or an assembly name, not both.");
        }

        if (assemblyOfType is null && assemblyName is null)
        {
            throw new ArgumentException("A full-name target requires an assembly symbol or an assembly name.");
        }

        Symbol = null;
        FullTypeName = fullTypeName;
        AssemblyOfType = assemblyOfType;
        AssemblyName = assemblyName;
    }

    public static RequestTarget ForSymbol(INamedTypeSymbol symbol)
        => new(symbol, null, null, null);

    public static RequestTarget ForFullNameWithAssemblyOfType(string fullTypeName, INamedTypeSymbol assemblyOfType)
        => new(null, fullTypeName, assemblyOfType, null);

    public static RequestTarget ForFullNameWithAssemblyName(string fullTypeName, string assemblyName)
        => new(null, fullTypeName, null, assemblyName);
}
