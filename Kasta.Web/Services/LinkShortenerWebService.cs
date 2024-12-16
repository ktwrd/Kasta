using Kasta.Data;
using Kasta.Data.Models;
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
        var user = await _userManager.GetUserAsync(controller.HttpContext.User);
        if (user == null && !string.IsNullOrEmpty(token))
        {
            var u = await _db.UserApiKeys.Where(e => e.Token == token).Include(e => e.User).FirstOrDefaultAsync();
            if (u != null)
            {
                user = u.User;
            }
        }
        if (user == null)
        {
            return DeleteShortenedLinkResult.NotAuthorized;
        }

        var model = await _db.ShortLinks.Where(e => e.Id == value).FirstOrDefaultAsync();
        model ??= await _db.ShortLinks.Where(e => e.ShortLink == value).FirstOrDefaultAsync();

        if (model == null)
        {
            return DeleteShortenedLinkResult.NotFound;
        }
        
        if (model.CreatedByUserId != user.Id)
        {
            var adminRoleId = await _db.Roles.Where(e => e.NormalizedName == RoleKind.Administrator.ToUpper()).Select(e => e.Id).FirstOrDefaultAsync();
            if (adminRoleId != null)
            {
                if (await _db.UserRoles.Where(e => e.UserId == user.Id && e.RoleId == adminRoleId).AnyAsync() == false)
                {
                    return DeleteShortenedLinkResult.NotAuthorized;
                }
            }
        }
        using (var ctx = _db.CreateSession())
        {
            var trans = await ctx.Database.BeginTransactionAsync();
            try
            {
                await ctx.ShortLinks.Where(e => e.Id == model.Id).ExecuteDeleteAsync();
                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                logger.LogError($"Failed to delete {nameof(ShortLinkModel)} where Id={model.Id}\n{ex}");
                throw;
            }
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