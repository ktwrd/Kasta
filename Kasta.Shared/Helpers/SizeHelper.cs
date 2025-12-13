using System.Text;
using System.Text.RegularExpressions;

namespace Kasta.Shared.Helpers;

public static class SizeHelper
{
    public static long? ParseToByteCount(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        if (long.TryParse(value.Trim(), out var directParse))
        {
            return directParse;
        }

        var actualRegex = new Regex(@"^([0-9]+(?:[,_][0-9]+){0,}((?:\.[0-9]+)?))([kmgt]i?b?|b|)$", RegexOptions.IgnoreCase);
        var match = actualRegex.Match(value.Trim());
        var t = match.Groups[^1].Value.Trim().ToLower();
        var numberValue = match.Groups[1].Value.Replace(",", "").Replace("_", "");
        long result = 0;
        if (numberValue.Contains('.'))
        {
            if (decimal.TryParse(numberValue, out var a))
            {
                var x = a;
                var c = t is [_, 'i', ..] ? 1000 : 1024;
                var r = t[0] switch
                {
                    'k' => 1,
                    'm' => 2,
                    'g' => 3,
                    't' => 4,
                    _ => 0
                };
                for (int i = 0; i < r; i++)
                {
                    x *= c;
                }

                result = Convert.ToInt64(Math.Max(Math.Round(x), 0));
            }
        }
        else
        {
            if (long.TryParse(numberValue, out var b))
            {
                var x = b;
                var c = t is [_, 'i', ..] ? 1000 : 1024;
                var r = t[0] switch
                {
                    'k' => 1,
                    'm' => 2,
                    'g' => 3,
                    't' => 4,
                    _ => 0
                };
                for (int i = 0; i < r; i++)
                {
                    x *= c;
                }

                result = x;
            }
        }
        return result;
    }
    
    [Obsolete("Please use NeoSmart.PrettySize.Bytes(byteCount).ToString()")]
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