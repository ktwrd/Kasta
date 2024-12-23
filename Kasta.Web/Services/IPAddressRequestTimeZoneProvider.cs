using Vivet.AspNetCore.RequestTimeZone.Providers;
using Vivet.AspNetCore.RequestTimeZone.Providers.Interfaces;

namespace Kasta.Web.Services;

public class IPAddressRequestTimeZoneProvider : IRequestTimeZoneProvider
{
    public Task<ProviderTimeZoneResult> DetermineProviderTimeZoneResult(HttpContext ctx)
    {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return Task.Run(() =>
        {
            var service = ctx.RequestServices.GetRequiredService<TimeZoneService>();
            var ip = ctx.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ip))
            {
                return default(ProviderTimeZoneResult);
            }
            var tzinfo = service.FromIpAddress(ip);
            if (tzinfo != null)
            {
                return new ProviderTimeZoneResult(tzinfo.Id);
            }
                return default(ProviderTimeZoneResult);
        });
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }
}