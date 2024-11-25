using System.Diagnostics;
using System.Text.RegularExpressions;

namespace kate.FileShare.Helpers;

public static class SizeHelper
{
    public static long? ParseToByteCount(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        var numberRegex = new Regex(@"^[0-9]+$");
        if (numberRegex.IsMatch(value.Trim()))
        {
            return Convert.ToInt64(value);
        }

        var actualRegex = new Regex(@"^([0-9]+(|(\.[0-9]+)))(b|k|m|g|t|kb|mb|gb|tb)$", RegexOptions.IgnoreCase);
        var match = actualRegex.Match(value.Trim());
        var t = match.Groups[match.Groups.Count - 1].Value;
        long result = 0;
        if (long.TryParse(match.Groups[1].Value, out var a))
        {
            if (t == "k" || t == "kb")
            {
                result = a * 1024;
            }
            else if (t == "m" || t == "mb")
            {
                result = a * 1024 * 1024;
            }
            else if (t == "g" || t == "gb")
            {
                result = a * 1024 * 1024 * 1024;
            }
            else if (t == "t" || t == "tb")
            {
                result = a * 1024 * 1024 * 1024 * 1024;
            }
        }
        return result;
    }
}