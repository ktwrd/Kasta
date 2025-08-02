using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

namespace Kasta.Web.Helpers;

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
        var t = match.Groups[^1].Value.ToLower();
        long result = 0;
        if (match.Groups[1].Value.Contains('.'))
        {
            if (decimal.TryParse(match.Groups[1].Value, out var a))
            {
                var x = a;
                if (t == "k" || t == "kb")
                {
                    x = a * 1024;
                }
                else if (t == "m" || t == "mb")
                {
                    x = a * 1024 * 1024;
                }
                else if (t == "g" || t == "gb")
                {
                    x = a * 1024 * 1024 * 1024;
                }
                else if (t == "t" || t == "tb")
                {
                    x = a * 1024 * 1024 * 1024 * 1024;
                }

                result = Convert.ToInt64(Math.Max(Math.Round(x), 0));
            }
        }
        else
        {
            if (long.TryParse(match.Groups[1].Value, out var b))
            {
                if (t == "k" || t == "kb")
                {
                    result = b * 1024;
                }
                else if (t == "m" || t == "mb")
                {
                    result = b * 1024 * 1024;
                }
                else if (t == "g" || t == "gb")
                {
                    result = b * 1024 * 1024 * 1024;
                }
                else if (t == "t" || t == "tb")
                {
                    result = b * 1024 * 1024 * 1024 * 1024;
                }
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

    public static long GetByteCount(string value)
    {
        return Encoding.UTF8.GetBytes(value).Length;
    }
}