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

    public static string ConnectionString => ParseString("DATABASE_CONNECTION_STRING", "");

    public static string S3ServiceUrl => ParseString("S3_ServiceUrl", "");
    public static string S3AccessKeyId => ParseString("S3_AccessKey", "");
    public static string S3AccessSecretKey => ParseString("S3_AccessSecret", "");
    public static bool S3ForcePathStyle => ParseBool("S3_ForcePathStyle", false);
    public static string S3BucketName => ParseString("S3_Bucket", "");
    public static string Endpoint => ParseString("DeploymentEndpoint", "http://localhost:5280");
}