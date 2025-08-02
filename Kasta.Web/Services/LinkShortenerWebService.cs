using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Services;

public class LinkShortenerWebService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;

    public LinkShortenerWebService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
    }

    public async Task<DeleteShortenedLinkResult> Delete<T>(ILogger<T> logger, string value, T controller, string? token = null)
        where T : Controller
    {
        var systemSettings = _db.GetSystemSettings();
        if (systemSettings.EnableLinkShortener == false)
        {
            return DeleteShortenedLinkResult.NotAuthorized;
        }
        var user = await _userManager.GetUserAsync(controller.HttpContext.User);
        if (user == null && !string.IsNullOrEmpty(token))
        {
            var u = await _db.UserApiKeys
                .AsNoTracking()
                .Where(e => e.Token == token)
                .Include(e => e.User)
                .FirstOrDefaultAsync();
            if (u != null)
            {
                user = u.User;
            }
        }
        if (user == null)
        {
            return DeleteShortenedLinkResult.NotAuthorized;
        }

        var model = await _db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == value);
        model ??= await _db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ShortLink == value);

        if (model == null)
        {
            return DeleteShortenedLinkResult.NotFound;
        }
        
        if (model.CreatedByUserId != user.Id)
        {
            var adminRoleId = await _db.Roles
                .Where(e => e.NormalizedName == RoleKind.Administrator.ToUpper())
                .Select(e => e.Id)
                .FirstOrDefaultAsync();
            if (adminRoleId != null)
            {
                if (await _db.UserRoles.Where(e => e.UserId == user.Id && e.RoleId == adminRoleId).AnyAsync() == false)
                {
                    return DeleteShortenedLinkResult.NotAuthorized;
                }
            }
        }

        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await ctx.ShortLinks.Where(e => e.Id == model.Id).ExecuteDeleteAsync();
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            logger.LogError(ex, "Failed to delete {0} where Id={ModelId}", nameof(ShortLinkModel), model.Id);
            throw;
        }

        return DeleteShortenedLinkResult.Success;
    }
    public enum DeleteShortenedLinkResult
    {
        Success,
        NotFound,
        NotAuthorized
    }
}