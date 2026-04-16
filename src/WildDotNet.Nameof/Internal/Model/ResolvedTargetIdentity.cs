namespace WildDotNet.Nameof.Internal.Model;

internal sealed record ResolvedTargetIdentity(
    string WrapperIdentitySource,
    string? NamespaceName,
    string TypeName);
