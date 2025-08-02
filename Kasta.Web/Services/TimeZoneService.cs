using System.Net;
using GeoTimeZone;
using Kasta.Data;
using MaxMind.GeoIP2;
using Microsoft.EntityFrameworkCore;

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

        if (_geoIpDatabase != null)
        {
            try
            {
                _geoIpDatabase?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to dispose {nameof(_geoIpDatabase)}");
            }
        }
        try
        {
            _geoIpDatabase = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to set {nameof(_geoIpDatabase)} to null");
        }
        try
        {
            _geoIpDatabaseLocation = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to set {nameof(_geoIpDatabaseLocation)} to null");
        }

        _disposed = true;
    }

    public TimeZoneInfo? FromCoordinates(double latitude, double longitude)
    {
        var result = TimeZoneLookup.GetTimeZone(latitude, longitude);
        var resultValue = result.Result.Trim();
        if (resultValue.Equals("utc", StringComparison.InvariantCultureIgnoreCase))
        {
            return TimeZoneInfo.Utc;
        }

        foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
        {
            if (Check(tz, resultValue))
            {
                return tz;
            }
            foreach (var inner in result.AlternativeResults)
            {
                if (Check(tz, inner.Trim()))
                {
                    return tz;
                }
            }
        }
        return null;

        bool Check(TimeZoneInfo tz, string req)
        {
            if (tz.Id.Equals(req, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            if (tz.StandardName.Equals(req, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(req, out var win))
            {
                if (tz.Id.Equals(win, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
    private readonly Lock _ensureMaxmindLock = new();
    private void EnsureMaxmind()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - 5 < _ensureMaxmindLast)
        {
            return;
        }
        _ensureMaxmindLast = now;
        var disable = _systemSettings.EnableGeoIp == false;
        if (disable && _geoIpDatabase == null)
        {
            _geoIpDatabaseLocation = null;
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
                if (_geoIpDatabaseLocation != _systemSettings.GeoIpDatabaseLocation)
                {
                    try
                    { _geoIpDatabase?.Dispose(); }
                    catch {}

                    _geoIpDatabase = new DatabaseReader(_systemSettings.GeoIpDatabaseLocation);
                    _geoIpDatabaseLocation = _systemSettings.GeoIpDatabaseLocation;
                }
            }
        }

        if (disable)
        {
            if (_geoIpDatabase != null)
            {
                try
                {
                    _geoIpDatabase.Dispose();
                }
                catch {}
                _geoIpDatabase = null;
            }
            _geoIpDatabaseLocation = null;
        }
    }

    private DatabaseReader? _geoIpDatabase;
    private string? _geoIpDatabaseLocation;
    private long _ensureMaxmindLast = 0;

    public TimeZoneInfo? FromIpAddress(string address)
    {
        lock (_ensureMaxmindLock)
        {
            EnsureMaxmind();
        }
        if (_geoIpDatabase == null) return null;

        if (!_geoIpDatabase.TryCity(address, out var city)) return null;
        
        var lat = city?.Location.Latitude;
        var lng = city?.Location.Longitude;
        if (lat != null && lng != null)
        {
            return FromCoordinates((double)lat, (double)lng);
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
            var headerMapping = _db.TrustedProxyHeaderMappings
                .AsNoTracking()
                .Where(e => e.TrustedProxyId == p.Id)
                .Where(e => e.TrustedProxyHeaderId != null)
                .ToList();
            foreach (var m in headerMapping)
            {
                var trustedProxyRecords = _db.TrustedProxyHeaders
                    .AsNoTracking()
                    .Where(e => e.Id == m.TrustedProxyHeaderId && e.Enable == true)
                    .ToList();
                foreach (var h in trustedProxyRecords)
                {
                    if (context.Request.Headers.TryGetValue(h.HeaderName, out var hv))
                    {
                        string[] headerValues;
                        if (hv.Count == 1 && hv.ToString().Contains(','))
                        {
                            headerValues = hv.ToString().Split(',').Select(e => e.Trim()).ToArray();
                        }
                        else
                        {
                            headerValues = (string[])hv.Where(e => e != null).ToArray()!;
                        }

                        if (headerValues.Length <= 0) continue;
                        
                        if (IPAddress.TryParse(headerValues[0], out var ipa))
                        {
                            return ipa.ToString();
                        }
                    }
                }
            }
        }
        return ip;
    }
}