using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Nameof.Internal.Support;

internal static class IdentifierUtilities
{
    public static IReadOnlyList<(string Identifier, string Value)> BuildMemberMap(IEnumerable<string> memberNames)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<(string Identifier, string Value)>();

        foreach (var value in memberNames.Where(static n => !string.IsNullOrWhiteSpace(n)))
        {
            var identifier = ToSafeIdentifier(value);
            identifier = EnsureUnique(identifier, used);
            result.Add((identifier, value));
        }

        return result;
    }

    public static string EscapeStringLiteral(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string ToSafeIdentifier(string memberName)
    {
        if (SyntaxFacts.IsValidIdentifier(memberName))
        {
            return IsKeyword(memberName) ? "@" + memberName : memberName;
        }

        var builder = new StringBuilder(memberName.Length + 8);

        if (memberName.Length == 0 || !IsIdentifierStart(memberName[0]))
        {
            builder.Append('_');
        }

        foreach (var ch in memberName)
        {
            builder.Append(IsIdentifierPart(ch) ? ch : '_');
        }

        var candidate = builder.ToString();
        if (SyntaxFacts.IsValidIdentifier(candidate))
        {
            return IsKeyword(candidate) ? "@" + candidate : candidate;
        }

        return "_" + candidate;
    }

    private static bool IsKeyword(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
               SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None;
    }

    private static bool IsIdentifierStart(char ch)
    {
        return ch == '_' || char.IsLetter(ch);
    }

    private static bool IsIdentifierPart(char ch)
    {
        return ch == '_' || char.IsLetterOrDigit(ch);
    }

    private static string EnsureUnique(string identifier, HashSet<string> used)
    {
        if (used.Add(identifier))
        {
            return identifier;
        }

        for (var i = 2; ; i++)
        {
            var candidate = identifier + "_" + i;
            if (used.Add(candidate))
            {
                return candidate;
            }
        }
    }
}
