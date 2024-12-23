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

    /// <summary>
    /// <para>Key: <c>OpenIdEnable</c></para>
    /// <para>Default Value: <see langword="false"/></para>
    /// </summary>
    public static bool OpenIdEnable => ParseBool("OpenIdEnable", false);
    /// <summary>
    /// <para>Key: <c>OpenIdClientId</c></para>
    /// <para>Default Value: (empty string)</para>
    /// </summary>
    public static string OpenIdClientId => ParseString("OpenIdClientId", "");
    /// <summary>
    /// <para>Key: <c>OpenIdClientSecret</c></para>
    /// <para>Default Value: (empty string)</para>
    /// </summary>
    public static string OpenIdClientSecret => ParseString("OpenIdClientSecret", "");
    /// <summary>
    /// <para>Key: <c>OpenIdEndpoint</c></para>
    /// <para>Default Value: (empty string)</para>
    /// </summary>
    public static string OpenIdEndpoint => ParseString("OpenIdEndpoint", "");
    /// <summary>
    /// <para>Key: <c>OpenIdValidateIssuer</c></para>
    /// <para>Default Value: <see langword="true"/></para>
    /// </summary>
    /// <remarks>Set this to <see langword="false"/> when using Authentik 2024.10 or later.</remarks>
    public static bool OpenIdValidateIssuer => ParseBool("OpenIdValidateIssuer", true);
    /// <summary>
    /// <para>Key: <c>OpenIdScopes</c></para>
    /// <para>Default Value: <c>openid profile</c></para>
    /// </summary>
    public static string OpenIdScopes => ParseString("OpenIdScopes", "openid profile");
    /// <summary>
    /// <para>Key: <c>OpenIdDisplayName</c></para>
    /// <para>Default Value: (empty string)</para>
    /// </summary>
    public static string OpenIdDisplayName => ParseString("OpenIdDisplayName", "");
    /// <summary>
    /// <para>Key: <c>OpenIdIdentifier</c></para>
    /// <para>Default Value: (empty string)</para>
    /// </summary>
    public static string OpenIdIdentifier => ParseString("OpenIdIdentifier", "");
    /// <summary>
    /// <para>Key: <c>JwtNameClaimType</c></para>
    /// <para>Default Value: <see cref="JwtRegisteredClaimNames.Name"/> (<c>name</c>)</para>
    /// </summary>
    public static string JwtNameClaimType => ParseString("JwtNameClaimType", "name");
    /// <summary>
    /// <para>Key: <c>JwtRoleClaimType</c></para>
    /// <para>Default Value: <c>roles</c></para>
    /// </summary>
    public static string JwtRoleClaimType => ParseString("JwtRoleClaimType", "roles");

    public static string SentryDsn => ParseString("SentryDsn", "");
}