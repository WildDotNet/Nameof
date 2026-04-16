using System.Collections.Generic;

namespace WildDotNet.Nameof.Internal.Model;

internal sealed record EmissionPlan(
    string? NamespaceName,
    string WrapperClassName,
    string WrapperHintIdentity,
    string ExtensionTargetFullyQualifiedTypeName,
    IReadOnlyCollection<string> MemberNames,
    bool IsOpenGenericDefinition = false,
    int GenericArity = 0,
    StubPlan? Stub = null);
