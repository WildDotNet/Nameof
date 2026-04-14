using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Nameof.Internal.Model;

namespace Nameof.Internal.Requests;

internal static class NameofRequestCollector
{
    public static ImmutableArray<NameofRequest> Collect(Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<NameofRequest>();

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (attribute.AttributeClass is not INamedTypeSymbol attributeClass)
            {
                continue;
            }

            if (!string.Equals(attributeClass.Name, "GenerateNameofAttribute", StringComparison.Ordinal))
            {
                continue;
            }

            if (attributeClass.Arity != 0)
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length == 1)
            {
                var arg0 = attribute.ConstructorArguments[0];
                if (arg0.Kind == TypedConstantKind.Type && arg0.Value is INamedTypeSymbol typeSymbol)
                {
                    builder.Add(new NameofRequest(typeSymbol, null, null, null));
                }

                continue;
            }

            if (attribute.ConstructorArguments.Length != 2)
            {
                continue;
            }

            var fullTypeNameArgument = attribute.ConstructorArguments[0];
            var assemblyArgument = attribute.ConstructorArguments[1];

            if (fullTypeNameArgument.Kind != TypedConstantKind.Primitive ||
                fullTypeNameArgument.Value is not string fullTypeName)
            {
                continue;
            }

            var resolved = compilation.GetTypeByMetadataName(fullTypeName);
            if (resolved is not null)
            {
                builder.Add(new NameofRequest(resolved, null, null, null));
                continue;
            }

            if (assemblyArgument.Kind == TypedConstantKind.Type &&
                assemblyArgument.Value is INamedTypeSymbol assemblyOfType)
            {
                builder.Add(new NameofRequest(null, fullTypeName, assemblyOfType, null));
                continue;
            }

            if (assemblyArgument.Kind == TypedConstantKind.Primitive &&
                assemblyArgument.Value is string assemblyName)
            {
                builder.Add(new NameofRequest(null, fullTypeName, null, assemblyName));
            }
        }

        return builder.ToImmutable();
    }
}
