using Vivet.AspNetCore.RequestTimeZone.Providers;
using Vivet.AspNetCore.RequestTimeZone.Providers.Interfaces;

namespace Kasta.Web.Services;

public class IpAddressRequestTimeZoneProvider : IRequestTimeZoneProvider
{
    public Task<ProviderTimeZoneResult?> DetermineProviderTimeZoneResult(HttpContext ctx)
    {
        return Task.Run(() =>
        {
            var service = ctx.RequestServices.GetRequiredService<TimeZoneService>();
            var ip = service.FindIpAddress(ctx);
            if (string.IsNullOrWhiteSpace(ip))
            {
                return null;
            }
            
            var timeZoneInfo = service.FromIpAddress(ip);
            if (timeZoneInfo != null)
            {
                return new ProviderTimeZoneResult(timeZoneInfo.Id);
            }

            return null;
        });
    }
}