namespace Kasta.Shared.Helpers;

public static class Extensions
{
    public static string FancyMaxLength(this string value, int maxLength = 50, bool includeEllipses = true)
    {
        if (value.Length < maxLength)
            return value;

        var targetLength = maxLength;
        
        if (maxLength >= 3)
        {
            targetLength -= 3;
        }

        var result = value[..targetLength];
        if (includeEllipses)
        {
            result += "...";
        }

        return result;
    }
}