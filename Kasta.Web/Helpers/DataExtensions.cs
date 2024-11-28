namespace Kasta.Web.Helpers;

public static class DataExtensions
{

    public static Kasta.Web.Models.SystemSettingsParams GetSystemSettings(this Kasta.Data.ApplicationDbContext context)
    {
        var instance = new Kasta.Web.Models.SystemSettingsParams();
        instance.Get(context);
        return instance;
    }
}