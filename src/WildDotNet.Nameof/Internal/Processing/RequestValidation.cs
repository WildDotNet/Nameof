namespace WildDotNet.Nameof.Internal.Processing;

internal static class RequestValidation
{
    public static bool IsSupportedNonGenericFullTypeName(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return false;
        }

        if (fullTypeName.IndexOf('`') >= 0 || fullTypeName.IndexOf('+') >= 0)
        {
            return false;
        }

        return fullTypeName.IndexOf('[') < 0 && fullTypeName.IndexOf(']') < 0;
    }
}
