using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Internal.Model;

namespace WildDotNet.Nameof.Internal.Resolvers;

internal interface ITypeMemberResolver
{
    bool CanResolve(ParsedNameofRequest request, Compilation compilation);

    ResolvedTypeShape? Resolve(ParsedNameofRequest request, Compilation compilation);
}
