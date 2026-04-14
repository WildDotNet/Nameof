using System.Runtime.CompilerServices;
using DiffEngine;

namespace Nameof.Tests.TestInfrastructure;

internal static class VerifySettings
{
    [ModuleInitializer]
    public static void Initialize()
    {
        DiffRunner.Disabled = true;
    }
}
