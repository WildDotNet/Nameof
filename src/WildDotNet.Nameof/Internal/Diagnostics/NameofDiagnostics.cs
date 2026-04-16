using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Internal.Model;

namespace WildDotNet.Nameof.Internal.Diagnostics;

internal static class NameofDiagnostics
{
    internal static DiagnosticDescriptor UnsupportedFullTypeNameDescriptor { get; } = new(
        id: "NAMEOF001",
        title: "Unsupported full type name",
        messageFormat: @"GenerateNameof(""{0}"") is not supported. Only non-generic, non-nested full type names are supported (example: ""Namespace.Type"").",
        category: "NameofGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static DiagnosticDescriptor ResolutionFailedUsingAssemblyOfDescriptor { get; } = new(
        id: "NAMEOF002",
        title: "Type resolution failed",
        messageFormat: @"Could not resolve type ""{0}"" using assemblyOf ""{1}"". No members were generated.",
        category: "NameofGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static DiagnosticDescriptor ResolutionFailedUsingAssemblyNameDescriptor { get; } = new(
        id: "NAMEOF003",
        title: "Type resolution failed",
        messageFormat: @"Could not resolve type ""{0}"" using assemblyName ""{1}"". No members were generated.",
        category: "NameofGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static DiagnosticDescriptor ClosedGenericTypeDescriptor { get; } = new(
        id: "NAMEOF004",
        title: "Closed generic types are not supported",
        messageFormat: @"GenerateNameof(""{0}"") is not supported. Only open generic definitions are supported. No members were generated.",
        category: "NameofGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static DiagnosticDescriptor DuplicateRequestDescriptor { get; } = new(
        id: "NAMEOF005",
        title: "Duplicate GenerateNameof request",
        messageFormat: @"GenerateNameof(""{0}"") is duplicated for assembly target ""{1}"". The duplicate request was ignored.",
        category: "NameofGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static Diagnostic CreateUnsupportedFullTypeName(Location? location, string fullTypeName)
        => Diagnostic.Create(UnsupportedFullTypeNameDescriptor, location, fullTypeName);

    internal static Diagnostic CreateResolutionFailedUsingAssemblyOf(
        ParsedNameofRequest request,
        string assemblyOfDisplayName)
        => Diagnostic.Create(
            ResolutionFailedUsingAssemblyOfDescriptor,
            request.AttributeLocation,
            request.Target.FullTypeName,
            assemblyOfDisplayName);

    internal static Diagnostic CreateResolutionFailedUsingAssemblyName(ParsedNameofRequest request)
        => Diagnostic.Create(
            ResolutionFailedUsingAssemblyNameDescriptor,
            request.AttributeLocation,
            request.Target.FullTypeName,
            request.Target.AssemblyName);

    internal static Diagnostic CreateClosedGeneric(ParsedNameofRequest request)
        => Diagnostic.Create(
            ClosedGenericTypeDescriptor,
            request.AttributeLocation,
            request.Target.DiagnosticDisplayName);

    internal static Diagnostic CreateDuplicateRequest(ParsedNameofRequest request)
        => Diagnostic.Create(
            DuplicateRequestDescriptor,
            request.AttributeLocation,
            request.Target.DiagnosticDisplayName,
            request.Target.DuplicateAssemblyTargetDisplayName);
}
