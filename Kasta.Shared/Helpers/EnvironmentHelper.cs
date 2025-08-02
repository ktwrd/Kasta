using System.ComponentModel;
using System.Text.RegularExpressions;
using NLog;

namespace Kasta.Shared.Helpers;

public static class EnvironmentHelper
{
    internal static EnvironmentFileHandler? Handler = null;
    internal static void ParseEnvData(bool force = false)
    {
        if (!force && Handler != null)
            return;
        var location = Environment.GetEnvironmentVariable("CUSTOM_ENV_LOCATION");
        if (string.IsNullOrEmpty(location))
        {
            Handler = new("", true);
            return;
        }

        Handler = new(location, true);
    }
    public static string? GetValue(string envKey)
    {
        ParseEnvData();
        return Handler!.FindValue(envKey);
    }
    /// <inheritdoc cref="EnvironmentFileHandler.GetBool(string, bool)"/>
    public static bool ParseBool(string environmentKey, bool defaultValue)
    {
        ParseEnvData();
        return Handler!.GetBool(environmentKey, defaultValue);
    }

    /// <inheritdoc cref="EnvironmentFileHandler.GetString(string, string)"/>
    public static string ParseString(string environmentKey, string defaultValue)
    {
        ParseEnvData();
        return Handler!.GetString(environmentKey, defaultValue);
    }

    /// <inheritdoc cref="EnvironmentFileHandler.GetStringArray(string, string[])"/>
    public static string[] ParseStringArray(string envKey, string[] defaultValue)
    {
        ParseEnvData();
        return Handler!.GetStringArray(envKey, defaultValue);
    }

    /// <inheritdoc cref="EnvironmentFileHandler.GetInt(string, int)"/>
    public static int ParseInt(string envKey, int defaultValue)
    {
        ParseEnvData();
        return Handler!.GetInt(envKey, defaultValue);
    }
}

internal class EnvironmentFileHandler
{
    public string Location { get; private set; }
    public EnvironmentFileHandler(string location, bool envAsFallback = true)
    {
        UseEnvironmentAsFallback = envAsFallback;
        Location = location;
        Parse();
    }
    [DefaultValue(true)]
    public bool UseEnvironmentAsFallback { get; private set; } = true;
    private readonly Dictionary<string, string> Values = [];
    internal string? FindValue(string key)
    {
        lock (Values)
        {
            foreach (var (k, v) in Values)
            {
                if (k.Trim().Equals(key.Trim(), StringComparison.InvariantCultureIgnoreCase))
                    return v;
            }
        }
        return UseEnvironmentAsFallback
            ? Environment.GetEnvironmentVariable(key)
            : null;
    }
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private void Parse()
    {
        if (!File.Exists(Location) || string.IsNullOrEmpty(Location))
        {
            _log.Warn($"File {Location} could not be found");
            return;
        }
        var content = File.ReadAllLines(Location);
        lock (Values)
        {
            Values.Clear();
            foreach (var item in content)
            {
                if (string.IsNullOrEmpty(item) || item.StartsWith('#'))
                    continue;
                string line = item;
                var commentIndex = line.IndexOf("#", StringComparison.Ordinal);
                if (commentIndex != -1)
                {
                    line = line.Substring(0, commentIndex);
                }
                if (string.IsNullOrEmpty(line))
                    continue;
                var idx = line.IndexOf("=", StringComparison.Ordinal);
                if (idx == -1)
                    continue;
                var key = line.Substring(0, idx);
                var value = line.Substring(idx + 1);
                if (value.StartsWith('"') && value.EndsWith('"'))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                Values[key] = value;
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    /// <summary>
    /// Parse an environment variable as <see cref="Int32"/>.
    /// </summary>
    /// <list type="bullet">
    /// <item>Fetch Environment variable (when null, set to <see cref="defaultValue"/> as string)</item>
    /// <item>Do regex match <c>^([0-9]+)$</c></item>
    /// <item>When success, parse item as integer then return</item>
    /// <item>When fail, return default value</item>
    /// </list>
    /// <param name="envKey">Environment key to get</param>
    /// <param name="defaultValue">Fallback value when not found</param>
    /// <param name="exists">Set to <see langword="true"/> when environment variable could not be found in the <c>.env</c> file or actual environment</param>
    public int GetInt(string envKey, int defaultValue, out bool exists)
    {
        var v = FindValue(envKey);
        exists = v != null;
        var item = v ?? defaultValue.ToString();
        var regex = new Regex(@"^([0-9]+)$");
        if (regex.IsMatch(item))
        {
            var match = regex.Match(item);
            var target = match.Groups[1].Value;
            return int.Parse(target);
        }
        _log.Warn($"Failed to parse {envKey} as integer (regex failed. value is \"{item}\"");
        return defaultValue;
    }
    /// <inheritdoc cref="GetInt(string, int, out bool)"/>
    public int GetInt(string envKey, int defaultValue)
    {
        return GetInt(envKey, defaultValue, out var _);
    }
    /// <summary>
    /// Get string from <c>.env</c> file or environment variables (when <see cref="UseEnvironmentAsFallback"/> is set to <see langword="true"/>)
    /// </summary>
    /// <param name="envKey">Environment key to check</param>
    /// <param name="defaultValue">Fallback value when not found</param>
    /// <param name="exists">Set to <see langword="true"/> when found in <c>.env</c> file or actual environment.</param>
    public string GetString(string envKey, string defaultValue, out bool exists)
    {
        var v = FindValue(envKey);
        exists = v != null;
        return v ?? defaultValue;
    }
    /// <inheritdoc cref="GetString(string, string, out bool)"/>
    public string GetString(string envKey, string defaultValue)
    {
        return GetString(envKey, defaultValue, out var _);
    }
    /// <summary>
    /// Parse environment variable into a string array, seperated by the `;` character
    /// </summary>
    /// <param name="envKey">Environment Key to search in</param>
    /// <param name="defaultValue">Default return value when null</param>
    /// <param name="exists">Set to <see langword="true"/> when exists in <c>.env</c> file or actual environment (when allowed)</param>
    /// <returns>Parsed string array</returns>
    public string[] GetStringArray(string envKey, string[] defaultValue, out bool exists)
    {
        var v = FindValue(envKey);
        exists = v != null;
        return GetString(envKey, string.Join(";", defaultValue)).Split(";").Where(v => !string.IsNullOrEmpty(v)).ToArray();
    }
    /// <inheritdoc cref="GetStringArray(string, string[], out bool)"/>
    public string[] GetStringArray(string envKey, string[] defaultValue)
    {
        return GetStringArray(envKey, defaultValue, out var _);
    }
    /// <summary>
    /// Parse environment variable as boolean from environment file, or <see cref="Environment"/> when <see cref="UseEnvironmentAsFallback"/> is set to <see langword="true"/>
    /// </summary>
    /// <param name="envKey">Environment Key to search in</param>
    /// <param name="defaultValue">Used when environment variable is not set.</param>
    /// <param name="exists">Set to true when the <paramref name="envKey"/> provided exists in the <c>.env</c> file or the actual environment (when allowed)</param>
    /// <returns><see langword="true"/> when value is <c>true</c> or <c>1</c>, <see langword="false"/> when value is <c>false</c> or <c>0</c>. Otherwise <paramref name="defaultValue"/> is returned.</returns>
    public bool GetBool(string envKey, bool defaultValue, out bool exists)
    {
        var v = FindValue(envKey);
        exists = v != null;

        var item = v ?? $"{defaultValue}";
        item = item.ToLower().Trim();

        if (item is "true" or "1")
            return true;
        else if (item is "false" or "0")
            return false;
        else
            return defaultValue;
    }
    /// <inheritdoc cref="GetBool(string, bool, out bool)"/>
    public bool GetBool(string envKey, bool defaultValue)
    {
        return GetBool(envKey, defaultValue, out var _);
    }
}