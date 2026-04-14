using Microsoft.CodeAnalysis;
using Nameof.Internal.Model;

namespace Nameof.Internal.Resolvers;

internal interface ITypeMemberResolver
{
    bool CanResolve(NameofRequest request, Compilation compilation);

    ResolvedNameofType? Resolve(NameofRequest request, Compilation compilation);
}
