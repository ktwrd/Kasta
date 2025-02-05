using System.Net;
using GeoTimeZone;
using Kasta.Data;
using Kasta.Web.Helpers;
using MaxMind.GeoIP2;

namespace Kasta.Web.Services;

public class TimeZoneService : IDisposable
{
    private static event Action? RefreshDatabase;
    public static void OnRefreshDatabase()
    {
        RefreshDatabase?.Invoke();
    }
    private readonly ApplicationDbContext _db;
    private readonly SystemSettingsProxy _systemSettings;

    private readonly ILogger<TimeZoneService> _logger;
    public TimeZoneService(IServiceProvider services, ILogger<TimeZoneService> logger)
    {
        _logger = logger;
        _db = services.GetRequiredService<ApplicationDbContext>();
        _systemSettings = services.GetRequiredService<SystemSettingsProxy>();
        RefreshDatabase += EnsureMaxmind;
        EnsureMaxmind();
    }
    private bool _disposed = false;
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            RefreshDatabase -= EnsureMaxmind;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to remove method {nameof(EnsureMaxmind)} from static event {nameof(RefreshDatabase)}");
        }

        if (_geoipDatabase != null)
        {
            try
            {
                _geoipDatabase?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to dispose {nameof(_geoipDatabase)}");
            }
        }
        try
        {
            _geoipDatabase = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to set {nameof(_geoipDatabase)} to null");
        }
        try
        {
            _geoipDatabaseLocation = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to set {nameof(_geoipDatabaseLocation)} to null");
        }

        _disposed = true;
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
    private object EnsureMaxmindLock = new object();
    private void EnsureMaxmind()
    {
        var tsnow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (tsnow - 5 < _ensureMaxmindLast)
        {
            return;
        }
        _ensureMaxmindLast = tsnow;
        bool disable = _systemSettings.EnableGeoIp == false;
        if (disable && _geoipDatabase == null)
        {
            _geoipDatabaseLocation = null;
            return;
        }
        if (_systemSettings.EnableGeoIp)
        {
            if (string.IsNullOrEmpty(_systemSettings.GeoIpDatabaseLocation))
            {
                disable = true;
            }
            else if (!File.Exists(_systemSettings.GeoIpDatabaseLocation))
            {
                disable = true;
            }
            else
            {
                if (_geoipDatabaseLocation != _systemSettings.GeoIpDatabaseLocation)
                {
                    try
                    { _geoipDatabase?.Dispose(); }
                    catch {}

                    _geoipDatabase = new DatabaseReader(_systemSettings.GeoIpDatabaseLocation);
                    _geoipDatabaseLocation = _systemSettings.GeoIpDatabaseLocation;
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
    private long _ensureMaxmindLast = 0;

    public TimeZoneInfo? FromIpAddress(string address)
    {
        lock (EnsureMaxmindLock)
        {
            EnsureMaxmind();
        }
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

    public string? FindIpAddress(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ip))
        {
            return null;
        }

        var trustedProxies = _db.TrustedProxies.Where(e => e.Address == ip && e.Enable == true).ToList();
        if (trustedProxies.Count == 0) return null;
        foreach (var p in trustedProxies)
        {
            var headerMapping = _db.TrustedProxyHeaderMappings.Where(e => e.TrustedProxyId == p.Id).Where(e => e.TrustedProxyHeaderId != null).ToList();
            foreach (var m in headerMapping)
            {
                foreach (var h in _db.TrustedProxyHeaders.Where(e => e.Id == m.TrustedProxyHeaderId && e.Enable == true).ToList())
                {
                    if (context.Request.Headers.TryGetValue(h.HeaderName, out var hv))
                    {
                        var hvs = Array.Empty<string>();
                        if (hv.Count == 1 && hv.ToString().Contains(","))
                        {
                            hvs = hv.ToString().Split(",").Select(e => e.Trim()).ToArray();
                        }
                        else
                        {
                            hvs = (string[])hv.Where(e => e != null).ToArray()!;
                        }
                        if (hvs.Length > 0)
                        {
                            if (IPAddress.TryParse(hvs[0], out var ipa))
                            {
                                return ipa.ToString();
                            }
                        }
                    }
                }
            }
        }
        return ip;
    }
}