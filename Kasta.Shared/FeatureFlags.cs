using Kasta.Shared.Helpers;

namespace Kasta.Shared;

public static class FeatureFlags
{
    #region Parsing
    /// <inheritdoc cref="EnvironmentHelper.ParseBool"/>
    private static bool ParseBool(string environmentKey, bool defaultValue)
    {
        return EnvironmentHelper.ParseBool(environmentKey, defaultValue);
    }

    /// <inheritdoc cref="EnvironmentHelper.ParseString"/>
    private static string ParseString(string environmentKey, string defaultValue)
    {
        return EnvironmentHelper.ParseString(environmentKey, defaultValue);
    }

    /// <inheritdoc cref="EnvironmentHelper.ParseStringArray"/>
    private static string[] ParseStringArray(string envKey, string[] defaultValue)
    {
        return EnvironmentHelper.ParseStringArray(envKey, defaultValue);
    }

    /// <inheritdoc cref="EnvironmentHelper.ParseInt"/>
    private static int ParseInt(string envKey, int defaultValue)
    {
        return EnvironmentHelper.ParseInt(envKey, defaultValue);
    }
    #endregion
    public static string Endpoint => KastaConfig.Get().Endpoint;

    public static string SentryDsn => ParseString("SentryDsn", "");

    public static string XmlConfigLocation => ParseString("CONFIG_LOCATION", RunningInDocker ? "/config/kasta.xml" : "./config.xml");
    public static bool RunningInDocker => ParseBool("_KASTA_RUNNING_IN_DOCKER", false);
    public static bool ShowPrivateInformationWithAspNet => ParseBool("AspNet_ShowPrivateInformation", false);
    public static bool SuppressPendingModelChangesWarning => ParseBool("EF_SuppressPendingModelChangesWarning", false);
}