using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace WildDotNet.Nameof.Internal.Policies;

internal readonly record struct StubKind(string TypeKeyword, string SealedKeyword, bool NeedsPrivateConstructor);

internal static class StubPolicy
{
    internal static bool NeedsStub(INamedTypeSymbol type)
    {
        return type.DeclaredAccessibility != Accessibility.Public &&
               !type.Locations.Any(static location => location.IsInSource);
    }

    internal static StubKind GetStubKind(INamedTypeSymbol type)
    {
        return type.TypeKind switch
        {
            TypeKind.Enum => new StubKind("enum", "", false),
            TypeKind.Interface => new StubKind("interface", "", false),
            TypeKind.Struct => new StubKind("struct", "", false),
            _ => new StubKind("class", " sealed", true)
        };
    }

    internal static StubKind GetStubKind(Type type)
    {
        if (type.IsEnum)
        {
            return new StubKind("enum", "", false);
        }

        if (type.IsInterface)
        {
            return new StubKind("interface", "", false);
        }

        if (type.IsValueType)
        {
            return new StubKind("struct", "", false);
        }

        return new StubKind("class", " sealed", true);
    }
}
