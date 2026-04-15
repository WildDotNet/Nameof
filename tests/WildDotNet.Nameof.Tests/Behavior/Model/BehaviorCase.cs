using Microsoft.CodeAnalysis;

namespace WildDotNet.Nameof.Tests.Behavior.Model;

internal sealed record BehaviorCase(
    string SnapshotName,
    string Source,
    MetadataReference[] References);

internal sealed record BehaviorScenarioCase(
    string SnapshotName,
    BehaviorCase ByType,
    BehaviorCase ByAssemblyName,
    BehaviorCase ByAssemblyOf);
