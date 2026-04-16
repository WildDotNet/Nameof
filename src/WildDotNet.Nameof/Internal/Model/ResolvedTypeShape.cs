namespace WildDotNet.Nameof.Internal.Model;

internal sealed record ResolvedTypeShape(
    ResolvedTargetIdentity Identity,
    ExtensionTarget ExtensionTarget,
    ResolvedMembers Members,
    StubPlan? Stub,
    bool IsOpenGenericDefinition = false,
    int GenericArity = 0);
