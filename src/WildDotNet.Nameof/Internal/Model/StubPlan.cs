using WildDotNet.Nameof.Internal.Policies;

namespace WildDotNet.Nameof.Internal.Model;

internal sealed record StubPlan(
    string Identity,
    string? NamespaceName,
    string TypeName,
    string? TypeParameters,
    StubKind Kind);
