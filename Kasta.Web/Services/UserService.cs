using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Services;

public class UserService
{
    private readonly ApplicationDbContext _db;
    public UserService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }
    
    /// <returns>
    /// <see langword="null"/> when quotas are disabled globally, or for this user.
    /// </returns>
    public async Task<long?> GetSpaceAvailableAsync(UserModel user)
    {
        var systemSettings = _db.GetSystemSettings();
        var userQuota = await _db.UserLimits
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == user.Id);
        if (systemSettings.EnableQuota)
        {
            if (userQuota?.MaxStorage >= 0)
            {
                return Math.Max((userQuota.MaxStorage - userQuota.SpaceUsed) ?? 0, 0);
            }
            else if (systemSettings.DefaultStorageQuotaReal >= 0)
            {
                if (userQuota == null)
                {
                    return systemSettings.DefaultStorageQuotaReal ?? 0;
                }
                else
                {
                    return Math.Max((systemSettings?.DefaultStorageQuotaReal - userQuota.SpaceUsed) ?? 0, 0);
                }
            }
        }
        return null;
    }
}