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

    public static string DatabaseHost => ParseString("DATABASE_HOST", "postgres");
    public static int DatabasePort => ParseInt("DATABASE_PORT", 5432);
    public static string DatabaseUser => ParseString("DATABASE_USER", "postgres");
    public static string DatabasePassword => ParseString("DATABASE_PASSWORD", "postgres");
    public static string DatabaseName => ParseString("DATABASE_NAME", "kasta");

    public static string S3ServiceUrl => ParseString("S3_ServiceUrl", "");
    public static string S3AccessKeyId => ParseString("S3_AccessKey", "");
    public static string S3AccessSecretKey => ParseString("S3_AccessSecret", "");
    public static bool S3ForcePathStyle => ParseBool("S3_ForcePathStyle", false);
    public static string S3BucketName => ParseString("S3_Bucket", "");
    public static string Endpoint => ParseString("DeploymentEndpoint", "http://localhost:5280");
    public static string DefaultRequestTimezone => ParseString("DefaultTimezone", "UTC");

    public static bool OpenIdEnable => ParseBool("OpenIdEnable", false);
    public static string OpenIdClientId => ParseString("OpenIdClientId", "");
    public static string OpenIdClientSecret => ParseString("OpenIdClientSecret", "");
    public static string OpenIdConfigurationUrl => ParseString("OpenIdConfigurationUrl", "");
    public static string OpenIdScopes => ParseString("OpenIdScopes", "openid profile");
    public static string OpenIdResponseType => ParseString("OpenIdResponseType", "code");

    public static bool JwtEnable => ParseBool("JwtEnable", false);
    public static bool JwtValidateIssuer => ParseBool("JwtValidateIssuer", true);
    public static bool JwtValidateAudience => ParseBool("JwtValidateAudience", true);
    public static bool JwtValidateLifetime => ParseBool("JwtValidateLifetime", true);
    public static bool JwtValidateIssuerSigningKey => ParseBool("JwtValidateIssuerSigningKey", true);
    public static string JwtValidIssuer => ParseString("JwtValidIssuer", "");
    public static string JwtValidAudience => ParseString("JwtValidAudience", "");
    public static string JwtIssuerSigningKeyLocation => ParseString("JwtIssuerSigningKeyLocation", "");
    public static string JwtIssuerSigningKeyBase64 => ParseString("JwtIssuerSigningKeyBase64", "");
}