using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using WildDotNet.Nameof.Internal.Model;
using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof.Internal.Requests;

internal static class NameofRequestParser
{
    public static ImmutableArray<ParsedNameofRequest> Parse(Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<ParsedNameofRequest>();

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
                        builder.Add(new ParsedNameofRequest(
                            RequestTarget.ForSymbol(typeSymbol),
                            RequestGenericInfo.ClosedGeneric(),
                            attributeLocation));
                    }
                    else if (TypeNameUtilities.IsOpenGenericDefinition(typeSymbol))
                    {
                        builder.Add(new ParsedNameofRequest(
                            RequestTarget.ForSymbol(typeSymbol.OriginalDefinition),
                            RequestGenericInfo.OpenDefinition(typeSymbol.Arity),
                            attributeLocation));
                    }
                    else
                    {
                        builder.Add(new ParsedNameofRequest(
                            RequestTarget.ForSymbol(typeSymbol),
                            RequestGenericInfo.NonGeneric(),
                            attributeLocation));
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
                    RequestGenericInfo.ClosedGeneric());
                continue;
            }

            if (TypeNameUtilities.TryGetOpenGenericArity(fullTypeName, out var genericArity))
            {
                var resolvedGeneric = compilation.GetTypeByMetadataName(fullTypeName);
                if (resolvedGeneric is not null)
                {
                    builder.Add(new ParsedNameofRequest(
                        RequestTarget.ForSymbol(resolvedGeneric.OriginalDefinition),
                        RequestGenericInfo.OpenDefinition(genericArity),
                        attributeLocation));
                    continue;
                }

                AddFullNameRequest(
                    builder,
                    fullTypeName,
                    assemblyArgument,
                    attributeLocation,
                    RequestGenericInfo.OpenDefinition(genericArity));
                continue;
            }

            var resolved = compilation.GetTypeByMetadataName(fullTypeName);
            if (resolved is not null)
            {
                builder.Add(new ParsedNameofRequest(
                    RequestTarget.ForSymbol(resolved),
                    RequestGenericInfo.NonGeneric(),
                    attributeLocation));
                continue;
            }

            AddFullNameRequest(
                builder,
                fullTypeName,
                assemblyArgument,
                attributeLocation,
                RequestGenericInfo.NonGeneric());
        }

        return builder.ToImmutable();
    }

    private static void AddFullNameRequest(
        ImmutableArray<ParsedNameofRequest>.Builder builder,
        string fullTypeName,
        TypedConstant assemblyArgument,
        Location? attributeLocation,
        RequestGenericInfo genericInfo)
    {
        if (assemblyArgument.Kind == TypedConstantKind.Type &&
            assemblyArgument.Value is INamedTypeSymbol assemblyOfType)
        {
            builder.Add(new ParsedNameofRequest(
                RequestTarget.ForFullNameWithAssemblyOfType(fullTypeName, assemblyOfType),
                genericInfo,
                attributeLocation));
            return;
        }

        if (assemblyArgument.Kind == TypedConstantKind.Primitive &&
            assemblyArgument.Value is string assemblyName)
        {
            builder.Add(new ParsedNameofRequest(
                RequestTarget.ForFullNameWithAssemblyName(fullTypeName, assemblyName),
                genericInfo,
                attributeLocation));
        }
    }
}
