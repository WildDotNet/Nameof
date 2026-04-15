using System.Collections.Generic;

namespace WildDotNet.Nameof.Internal.Model;

internal sealed record ResolvedNameofType(
    string FullTypeName,
    string? NamespaceName,
    string TypeName,
    bool EmitStub,
    string? StubIdentity,
    string WrapperClassName,
    string ExtensionTargetFullyQualifiedTypeName,
    IReadOnlyCollection<string> MemberNames,
    bool IsOpenGenericDefinition = false,
    int GenericArity = 0,
    string? TypeParameters = null,
    (string TypeKeyword, string SealedKeyword, bool NeedsPrivateConstructor)? StubKind = null);
