using System.Collections.Generic;

namespace WildDotNet.Nameof.Internal.Model;

internal sealed record ResolvedMembers(
    IReadOnlyCollection<string> Names);
