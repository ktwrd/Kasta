using System.Text;
using System.Text.RegularExpressions;

namespace Kasta.Shared.Helpers;

public partial class SizeHelper
{
    [GeneratedRegex(@"^([0-9]+(?:[,_][0-9]+){0,}((?:\.[0-9]+)?))[\s]*([kmgt]i?b?|b|)$", RegexOptions.IgnoreCase, "en-AU")]
    private static partial Regex ParseToBytesExpression();
    
    /// <summary>
    /// <para>
    /// Parse the provided <paramref name="value"/> into it's respective bytes (e.g: 3.5MB is 3,670,016b)
    /// </para>
    /// 
    /// If the multiplied decimal resolves to something like <c>103321.6</c> then it will always round up.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the number part of <paramref name="value"/> cannot be parsed
    /// into a <see cref="decimal"/> or <see cref="long"/>
    /// </exception>
    public static long? ParseToByteCount(string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value))
            return null;
        if (long.TryParse(value, out var directParse))
        {
            return directParse;
        }

        var match = ParseToBytesExpression().Match(value);
        var t = match.Groups[^1].Value.Trim().ToLower();
        var numberValue = match.Groups[1].Value.Replace(",", "").Replace("_", "");
        long result = 0;
        var multReps = 0;
        var mult = t is [_, 'i', ..] ? 1000 : 1024;
        if (t.Length > 0)
        {
            multReps = t[0] switch
            {
                'k' => 1,
                'm' => 2,
                'g' => 3,
                't' => 4,
                _ => 0
            };
        }
        if (numberValue.Contains('.'))
        {
            if (decimal.TryParse(numberValue, out var @decimal))
            {
                var x = @decimal;
                for (int i = 0; i < multReps; i++)
                {
                    x *= mult;
                }

                result = Convert.ToInt64(Math.Max(Math.Ceiling(x), 0));
            }
            else
            {
                throw new ArgumentException($"Could not parse value \"{value}\" into {typeof(decimal)}", nameof(value));
            }
        }
        else
        {
            if (long.TryParse(numberValue, out var @long))
            {
                var x = @long;
                for (int i = 0; i < multReps; i++)
                {
                    x *= mult;
                }

                result = x;
            }
            else
            {
                throw new ArgumentException($"Could not parse value \"{value}\" into {typeof(long)}", nameof(value));
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