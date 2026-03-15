namespace Kasta.Web;

public static class HttpContextExtensions
{
    public static bool IsHtmxRequest(this HttpContext context)
    {
        var hxRequest = context.Request.Headers
            .Where(e => string.Equals(e.Key, "hx-request", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Value)
            .FirstOrDefault();
        return hxRequest.Any(e
            => string.Equals(e?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
            || (int.TryParse(e, out var ei) && ei >= 1));
    }
}
