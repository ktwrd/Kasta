namespace Kasta.Web;

public static class HttpContextExtensions
{
    public static bool IsHtmxRequest(this HttpContext context)
    {
        return context.Request.Headers
            .Where(e => string.Equals(e.Key, "hx-request", StringComparison.OrdinalIgnoreCase))
            .SelectMany(e => e.Value)
            .Any(e => string.Equals(e?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
                      || (int.TryParse(e, out var ei) && ei >= 1));
    }
}
