using GeoTimeZone;
using Kasta.Data;
using Kasta.Web.Helpers;
using MaxMind.GeoIP2;

namespace Kasta.Web.Services;

public class TimeZoneService : IDisposable
{
    public static event Action? RefreshDatabase;
    private readonly ApplicationDbContext _db;

    public TimeZoneService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        RefreshDatabase += EnsureMaxmind;
    }
    public void Dispose()
    {
        RefreshDatabase -= EnsureMaxmind;
        try
        { _geoipDatabase?.Dispose(); }
        catch {}
    }

    public TimeZoneInfo? FromCoordinates(double latitude, double longitude)
    {
        var result = TimeZoneLookup.GetTimeZone(latitude, longitude);
        string TrimLower(string f)
        {
            return f.Trim().ToLower();
        }
        if (result.Result.Trim().ToLower() == "utc")
        {
            return TimeZoneInfo.Utc;
        }

        bool Check(TimeZoneInfo tz, string req)
        {
            if (TrimLower(tz.Id) == req)
            {
                return true;
            }
            if (TrimLower(tz.StandardName) == req)
            {
                return true;
            }
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(req, out var win))
            {
                if (TrimLower(tz.Id) == TrimLower(win))
                {
                    return true;
                }
            }
            return false;
        }
        
        var detectedTimezone = TrimLower(result.Result);
        foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
        {
            if (Check(tz, detectedTimezone))
            {
                return tz;
            }
            foreach (var inner in result.AlternativeResults)
            {
                if (Check(tz, TrimLower(inner)))
                {
                    return tz;
                }
            }
        }
        return null;
    }

    private void EnsureMaxmind()
    {
        var sys = _db.GetSystemSettings();
        bool disable = sys.EnableGeoIP == false;
        if (disable && _geoipDatabase == null)
        {
            _geoipDatabaseLocation = null;
            return;
        }
        if (sys.EnableGeoIP)
        {
            if (string.IsNullOrEmpty(sys.GeoIPDatabaseLocation))
            {
                disable = true;
            }
            else if (!File.Exists(sys.GeoIPDatabaseLocation))
            {
                disable = true;
            }
            else
            {
                if (_geoipDatabaseLocation != sys.GeoIPDatabaseLocation)
                {
                    try
                    { _geoipDatabase?.Dispose(); }
                    catch {}

                    _geoipDatabase = new DatabaseReader(sys.GeoIPDatabaseLocation);
                    _geoipDatabaseLocation = sys.GeoIPDatabaseLocation;
                }
            }
        }

        if (disable)
        {
            if (_geoipDatabase != null)
            {
                try
                {
                    _geoipDatabase.Dispose();
                }
                catch {}
                _geoipDatabase = null;
            }
            _geoipDatabaseLocation = null;
        }
    }

    private DatabaseReader? _geoipDatabase;
    private string? _geoipDatabaseLocation;

    public TimeZoneInfo? FromIpAddress(string address)
    {
        EnsureMaxmind();
        if (_geoipDatabase == null) return null;

        if (_geoipDatabase.TryCity(address, out var city))
        {
            var lat = city?.Location.Latitude;
            var lng = city?.Location.Longitude;
            if (lat != null && lng != null)
            {
                return FromCoordinates((double)lat, (double)lng);
            }
        }
        return null;
    }
}