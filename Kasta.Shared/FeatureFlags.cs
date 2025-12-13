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
    
    public static string Endpoint => KastaConfig.Instance.Endpoint;

    public static string SentryDsn => ParseString(Keys.SentryDsn, "");
    public static string XmlConfigLocation => ParseString(Keys.XmlConfigLocation, RunningInDocker ? "/config/kasta.xml" : "./config.xml");
    public static bool RunningInDocker => ParseBool(Keys.RunningInDocker, false);
    public static bool ShowPrivateInformationWithAspNet => ParseBool(Keys.ShowPrivateInformationWithAspNet, false);
    public static bool SuppressPendingModelChangesWarning => ParseBool(Keys.SuppressPendingModelChangesWarning, false);

    public static class Keys
    {
        public const string SentryDsn = "SentryDsn";
        public const string XmlConfigLocation = "CONFIG_LOCATION";
        public const string RunningInDocker = "_KASTA_RUNNING_IN_DOCKER";
        public const string ShowPrivateInformationWithAspNet = "AspNet_ShowPrivateInformation";
        public const string SuppressPendingModelChangesWarning = "EF_SuppressPendingModelChangesWarning";
    }
}