using Kasta.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Kasta.Web;

partial class Startup
{
    private static void ConfigureAuthenticationServices(IServiceCollection services)
    {
        var cfg = KastaConfig.Instance;
        if (cfg.Auth == null || cfg.Auth.OAuth.Count < 1) return;
        
        var auth = services.AddAuthentication()
            .AddCookie(JwtBearerDefaults.AuthenticationScheme);
        foreach (var item in cfg.Auth!.OAuth)
        {
            auth.AddOpenIdConnect(
                item.Identifier,
                item.DisplayName,
                options =>
                {
                    ConfigureGenericOpenIdConnect(item, options);
                });
        }
    }
    private static void ConfigureGenericOpenIdConnect(
        GenericOAuthConfig config,
        OpenIdConnectOptions options)
    {
        options.RequireHttpsMetadata = false;
        options.ClientId = config.ClientId;
        options.ClientSecret = config.ClientSecret;
        options.Authority = config.Endpoint;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.ResponseMode = "query";
        options.Scope.Clear();
        foreach (var x in config.Scopes)
        {
            options.Scope.Add(x);
        }
        options.SaveTokens = true;
        // options.GetClaimsFromUserInfoEndpoint = true;
        options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
        options.TokenValidationParameters.RoleClaimType = "roles";
        if (config.UseTokenLifetime.HasValue)
        {
            options.UseTokenLifetime = config.UseTokenLifetime.Value;
        }
        foreach (var inner in config.Jwt?.Items ?? [])
        {
            switch (inner.InternalName)
            {
                case "name":
                    options.TokenValidationParameters.NameClaimType = inner.JwtValue;
                    break;
                case "role":
                    options.TokenValidationParameters.RoleClaimType = inner.JwtValue;
                    break;
            }
        }
        if (!config.ValidateIssuer)
        {
            options.TokenValidationParameters.ValidateIssuerSigningKey = false;
            options.TokenValidationParameters.SignatureValidator
                = (a, _) => new JsonWebToken(a);
        }

        // added in v0.9.2
        options.Events.OnRedirectToIdentityProvider += OpenIdEnsureHttpsRedirect;
        options.Events.OnRedirectToIdentityProviderForSignOut += OpenIdEnsureHttpsRedirect;
    }

    // added in v0.9.2
    private static Task OpenIdEnsureHttpsRedirect(RedirectContext ctx)
    {
        const string http = "http://";
        const string https = "https://";
        if (KastaConfig.Instance.Endpoint.StartsWith(https, StringComparison.OrdinalIgnoreCase) &&
            ctx.ProtocolMessage.RedirectUri.StartsWith(http, StringComparison.OrdinalIgnoreCase))
        {
            ctx.ProtocolMessage.RedirectUri = https + ctx.ProtocolMessage.RedirectUri[https.Length..];
        }
        return Task.CompletedTask;
    }
}