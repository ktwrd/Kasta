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

    
    /// <summary>
    /// <para><b>Key:</b> <c>ASPNET_ENVIRONMENT</c></para>
    /// </summary>
    public static string AspNetEnvironment => ParseString(Keys.AspNetEnvironment, "");
    /// <summary>
    /// <para><b>Key:</b> <c>DOTNET_ENVIRONMENT</c></para>
    /// </summary>
    public static string DotNetEnvironment => ParseString(Keys.DotNetEnvironment, "");

    public const string EnvironmentValueDevelopment = "DEVELOPMENT";
    
    /// <summary>
    /// Check if either <see cref="Keys.AspNetEnvironment"/> or <see cref="Keys.DotNetEnvironment"/>
    /// equals <c>DEVELOPMENT</c> (case-insensitive)
    /// </summary>
    public static bool IsDevelopmentEnvironment
        => string.Equals(AspNetEnvironment.Trim(), EnvironmentValueDevelopment, StringComparison.InvariantCultureIgnoreCase)
        || string.Equals(DotNetEnvironment.Trim(), EnvironmentValueDevelopment, StringComparison.InvariantCultureIgnoreCase);
    public static void SetEnvironment(string value)
    {
        Environment.SetEnvironmentVariable(Keys.AspNetEnvironment, value);
        Environment.SetEnvironmentVariable(Keys.DotNetEnvironment, value);
    }
    
    /// <summary>
    /// <para>Make sure that <see cref="AspNetEnvironment"/> and <see cref="DotNetEnvironment"/>
    /// equal to whatever one is set, when the other isn't set.</para>
    ///
    /// When <see cref="AspNetEnvironment"/> is set, and <see cref="DotNetEnvironment"/> isn't set, then this will set the value for <see cref="DotNetEnvironment"/> to be equal to <see cref="AspNetEnvironment"/>.
    /// Same thing is done, but with the environment variables swapped.
    /// </summary>
    public static void EnsureEnvironmentValue()
    {
        const string prefix = $"[{nameof(FeatureFlags)}.{nameof(EnsureEnvironmentValue)}]";
        if (string.IsNullOrEmpty(DotNetEnvironment) &&
            !string.IsNullOrEmpty(AspNetEnvironment))
        {
            System.Diagnostics.Trace.WriteLine($"{prefix} Updated {Keys.DotNetEnvironment} to match {Keys.AspNetEnvironment} ({AspNetEnvironment})");
            SetEnvironment(AspNetEnvironment);
        }
        else if (!string.IsNullOrEmpty(DotNetEnvironment) &&
                 string.IsNullOrEmpty(AspNetEnvironment))
        {
            System.Diagnostics.Trace.WriteLine($"{prefix} Updated {Keys.AspNetEnvironment} to match {Keys.DotNetEnvironment} ({DotNetEnvironment})");
            SetEnvironment(DotNetEnvironment);
        }
        else if (string.IsNullOrEmpty(DotNetEnvironment) && string.IsNullOrEmpty(AspNetEnvironment))
        {
            System.Diagnostics.Trace.WriteLine($"{prefix} {Keys.AspNetEnvironment} and {Keys.DotNetEnvironment} aren't set.");
        }
        else if (!string.IsNullOrEmpty(DotNetEnvironment) && !string.IsNullOrEmpty(AspNetEnvironment) &&
                 !DotNetEnvironment.Equals(AspNetEnvironment, StringComparison.InvariantCultureIgnoreCase))
        {
            System.Diagnostics.Trace.WriteLine(string.Join(Environment.NewLine,
                $"{prefix} {Keys.AspNetEnvironment} and {Keys.DotNetEnvironment} are set to different values!!!",
                $"{Keys.AspNetEnvironment}: {AspNetEnvironment}",
                $"{Keys.DotNetEnvironment}: {DotNetEnvironment}"));
        }
    }
    
    public static class Keys
    {
        public const string SentryDsn = "SentryDsn";
        public const string XmlConfigLocation = "CONFIG_LOCATION";
        public const string RunningInDocker = "_KASTA_RUNNING_IN_DOCKER";
        public const string ShowPrivateInformationWithAspNet = "AspNet_ShowPrivateInformation";
        public const string SuppressPendingModelChangesWarning = "EF_SuppressPendingModelChangesWarning";

        public const string AspNetEnvironment = "ASPNET_ENVIRONMENT";
        public const string DotNetEnvironment = "DOTNET_ENVIRONMENT";
    }
}