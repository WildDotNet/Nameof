using System;

namespace WildDotNet.Nameof.Internal.Model;

internal readonly record struct RequestGenericInfo
{
    private enum Kind
    {
        NonGeneric,
        OpenDefinition,
        ClosedGeneric
    }

    private readonly Kind _kind;

    public int Arity { get; }

    public bool IsOpenDefinition => _kind == Kind.OpenDefinition;

    public bool IsClosedGeneric => _kind == Kind.ClosedGeneric;

    private RequestGenericInfo(Kind kind, int arity)
    {
        if (kind == Kind.OpenDefinition && arity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arity), "Open generic definitions require a positive arity.");
        }

        if (kind != Kind.OpenDefinition && arity != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arity), "Only open generic definitions can carry arity.");
        }

        _kind = kind;
        Arity = arity;
    }

    public static RequestGenericInfo NonGeneric()
        => new(Kind.NonGeneric, 0);

    public static RequestGenericInfo OpenDefinition(int arity)
        => new(Kind.OpenDefinition, arity);

    public static RequestGenericInfo ClosedGeneric()
        => new(Kind.ClosedGeneric, 0);
}
