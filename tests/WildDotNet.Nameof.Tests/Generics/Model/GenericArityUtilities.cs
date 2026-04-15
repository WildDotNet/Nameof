namespace WildDotNet.Nameof.Tests.Generics.Model;

internal static class GenericArityUtilities
{
    internal static int GetArityValue(Arity arity)
    {
        return arity switch
        {
            Arity.Arity1 => 1,
            Arity.Arity2 => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(arity)),
        };
    }

    internal static string GetTypeParameters(Arity arity)
    {
        return arity switch
        {
            Arity.Arity1 => "<T>",
            Arity.Arity2 => "<T1, T2>",
            _ => throw new ArgumentOutOfRangeException(nameof(arity)),
        };
    }

    internal static string GetOpenGenericTypeArguments(Arity arity)
    {
        return arity switch
        {
            Arity.Arity1 => "<>",
            Arity.Arity2 => "<,>",
            _ => throw new ArgumentOutOfRangeException(nameof(arity)),
        };
    }

    internal static string GetMetadataName(string typeName, Arity arity)
    {
        return $"{typeName}`{GetArityValue(arity)}";
    }
}
