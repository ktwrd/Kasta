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
        var t = match.Groups[match.Groups.Count - 1].Value.ToLower();
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
    
    public static string BytesToString(long byteCount)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB" }; //Longs run out around EB
        if (byteCount == 0)
            return "0" + suf[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + suf[place];
    }
}