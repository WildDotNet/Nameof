using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Internal.Model;
using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof.Internal.Requests;

internal static class NameofRequestCollector
{
    public static ImmutableArray<NameofRequest> Collect(Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<NameofRequest>();

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            var attributeLocation = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();

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
                    if (TypeNameUtilities.IsClosedConstructedGenericType(typeSymbol))
                    {
                        builder.Add(new NameofRequest(
                            typeSymbol,
                            null,
                            null,
                            null,
                            attributeLocation,
                            IsClosedGeneric: true));
                    }
                    else if (TypeNameUtilities.IsOpenGenericDefinition(typeSymbol))
                    {
                        builder.Add(new NameofRequest(
                            typeSymbol.OriginalDefinition,
                            null,
                            null,
                            null,
                            attributeLocation,
                            IsOpenGenericDefinition: true,
                            GenericArity: typeSymbol.Arity));
                    }
                    else
                    {
                        builder.Add(new NameofRequest(typeSymbol, null, null, null, attributeLocation));
                    }
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

            if (TypeNameUtilities.IsClosedGenericTypeName(fullTypeName))
            {
                AddFullNameRequest(
                    builder,
                    fullTypeName,
                    assemblyArgument,
                    attributeLocation,
                    isOpenGenericDefinition: false,
                    isClosedGeneric: true,
                    genericArity: 0);
                continue;
            }

            if (TypeNameUtilities.TryGetOpenGenericArity(fullTypeName, out var genericArity))
            {
                var resolvedGeneric = compilation.GetTypeByMetadataName(fullTypeName);
                if (resolvedGeneric is not null)
                {
                    builder.Add(new NameofRequest(
                        resolvedGeneric.OriginalDefinition,
                        null,
                        null,
                        null,
                        attributeLocation,
                        IsOpenGenericDefinition: true,
                        GenericArity: genericArity));
                    continue;
                }

                AddFullNameRequest(
                    builder,
                    fullTypeName,
                    assemblyArgument,
                    attributeLocation,
                    isOpenGenericDefinition: true,
                    isClosedGeneric: false,
                    genericArity: genericArity);
                continue;
            }

            var resolved = compilation.GetTypeByMetadataName(fullTypeName);
            if (resolved is not null)
            {
                    builder.Add(new NameofRequest(resolved, null, null, null, attributeLocation));
                continue;
            }

            AddFullNameRequest(
                builder,
                fullTypeName,
                assemblyArgument,
                attributeLocation,
                isOpenGenericDefinition: false,
                isClosedGeneric: false,
                genericArity: 0);
        }

        return builder.ToImmutable();
    }

    private static void AddFullNameRequest(
        ImmutableArray<NameofRequest>.Builder builder,
        string fullTypeName,
        TypedConstant assemblyArgument,
        Location? attributeLocation,
        bool isOpenGenericDefinition,
        bool isClosedGeneric,
        int genericArity)
    {
        if (assemblyArgument.Kind == TypedConstantKind.Type &&
            assemblyArgument.Value is INamedTypeSymbol assemblyOfType)
        {
            builder.Add(new NameofRequest(
                null,
                fullTypeName,
                assemblyOfType,
                null,
                attributeLocation,
                IsOpenGenericDefinition: isOpenGenericDefinition,
                IsClosedGeneric: isClosedGeneric,
                GenericArity: genericArity));
            return;
        }

        if (assemblyArgument.Kind == TypedConstantKind.Primitive &&
            assemblyArgument.Value is string assemblyName)
        {
            builder.Add(new NameofRequest(
                null,
                fullTypeName,
                null,
                assemblyName,
                attributeLocation,
                IsOpenGenericDefinition: isOpenGenericDefinition,
                IsClosedGeneric: isClosedGeneric,
                GenericArity: genericArity));
        }
    }
}
