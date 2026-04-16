using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof.Internal.Model;

internal static class EmissionPlanFactory
{
    public static EmissionPlan Create(ResolvedTypeShape shape)
    {
        var typeIdentity = TypeNameUtilities.GetTypeIdentity(shape.Identity.WrapperIdentitySource);
        var wrapperClassName = shape.IsOpenGenericDefinition
            ? $"NameofGeneric_{typeIdentity}"
            : $"Nameof_{typeIdentity}";
        var wrapperHintIdentity = shape.IsOpenGenericDefinition
            ? $"Generic.{typeIdentity}"
            : typeIdentity;

        return new EmissionPlan(
            shape.Identity.NamespaceName,
            wrapperClassName,
            wrapperHintIdentity,
            shape.ExtensionTarget.FullyQualifiedTypeName,
            shape.Members.Names,
            shape.IsOpenGenericDefinition,
            shape.GenericArity,
            shape.Stub);
    }
}
