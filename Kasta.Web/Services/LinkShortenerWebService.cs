using System.ComponentModel;
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
    private readonly UserService _userService;
    private readonly UserManager<UserModel> _userManager;
    private readonly SystemSettingsProxy _systemSettings;

    public LinkShortenerWebService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userService = services.GetRequiredService<UserService>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _systemSettings = services.GetRequiredService<SystemSettingsProxy>();
    }

    public async Task<DeleteShortenedLinkResult> Delete<T>(ILogger<T> logger, string value, T controller, string? token = null)
        where T : Controller
    {
        if (!_systemSettings.EnableLinkShortener)
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

    public async Task<CreateShortenedLinkResult> Create<TController>(
        ILogger<TController> logger,
        TController controller,
        string url,
        string? vanity = null)
    where TController : Controller
    {
        var user = await _userService.GetCurrentUser(controller.HttpContext);
        if (user == null)
        {
            return CreateShortenedLinkResult.NotAuthorizedNotLoggedIn;
        }

        if (!_systemSettings.EnableLinkShortener)
        {
            return CreateShortenedLinkResult.NotAuthorizedFeatureDisabled;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return  CreateShortenedLinkResult.InvalidUri;
        }

        vanity = vanity?.Trim().ToLower();
        if (!string.IsNullOrEmpty(vanity))
        {
            if (!await _userManager.IsInRoleAsync(user, RoleKind.LinkShortenerCreateVanity))
            {
                return CreateShortenedLinkResult.NotAuthorizedMissingVanityCreatorRole;
            }
            if (await _db.ShortLinks.AnyAsync(e => e.ShortLink == vanity))
            {
                return CreateShortenedLinkResult.VanityAlreadyExists;
            }
        }

        ShortLinkModel result;
        using var trans = await _db.Database.BeginTransactionAsync();
        try
        {

            await _db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            logger.LogError(ex, "Failed to create shortened link {Url} for user {UserEmail} ({UserId})",
                url,
                user.Email,
                user.Id);
            throw;
        }

        throw new NotImplementedException();
    }

    public enum CreateShortenedLinkResult
    {
        [Description("Successfully created link!")]
        Success,
        [Description("Vanity URL already exists")]
        VanityAlreadyExists,
        [Description("Not Authorized - Please login")]
        NotAuthorizedNotLoggedIn,
        [Description("Not Authorized - You cannot create vanity URLs")]
        NotAuthorizedMissingVanityCreatorRole,
        [Description("Not Authorized - Link Shortener is disabled")]
        NotAuthorizedFeatureDisabled,
        InvalidUri
    }
}