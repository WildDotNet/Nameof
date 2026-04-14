using System.Collections.Generic;

namespace Nameof.Internal.Model;

internal sealed record ResolvedNameofType(
    string FullTypeName,
    string? NamespaceName,
    string TypeName,
    bool EmitStub,
    string WrapperClassName,
    string FullyQualifiedTypeName,
    IReadOnlyCollection<string> MemberNames,
    string? TypeParameters = null,
    (string TypeKeyword, string SealedKeyword, bool NeedsPrivateConstructor)? StubKind = null);
