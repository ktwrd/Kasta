using System.ComponentModel;
using System.Reflection;

namespace Kasta.Shared;

public static class Extensions
{
    /// <summary>
    /// Get the value of the <see cref="DescriptionAttribute"/> on an object, if it is on it.
    /// </summary>
    /// <returns>Empty string when no <see cref="DescriptionAttribute"/> found</returns>
    public static string ToDescriptionString<T>(this T value, string fallback) where T : struct
    {
        if (value.ToString() == null)
            return fallback;
        var attributes = (DescriptionAttribute[])value
            .GetType()
            .GetField(value.ToString())
            .GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes?.Length > 0 ? attributes[0].Description : fallback;
    }

    /// <summary>
    /// Get the value of the <see cref="DescriptionAttribute"/> on an object, if it is on it.
    /// </summary>
    /// <returns>Empty string when no <see cref="DescriptionAttribute"/> found</returns>
    public static string ToDescriptionString<T>(this T value) where T : struct
    {
        return value.ToDescriptionString(string.Empty);
    }

    public static string? ToCodeValue<T>(this T value) where T : struct
    {
        return value.ToCodeValue<T>(null);
    }
    public static string? ToCodeValue<T>(this T value, string? fallback) where T : struct
    {
        var attributes = value
            .GetType()
            .GetField(value.ToString()!)?
            .GetCustomAttributes<CodeAttribute>(false);
        return attributes?.FirstOrDefault()?.Code ?? fallback;
    }
}