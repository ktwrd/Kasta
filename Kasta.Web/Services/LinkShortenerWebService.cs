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
    private readonly KastaDbContext _db;
    private readonly UserService _userService;
    private readonly UserManager<UserModel> _userManager;
    private readonly SystemSettingsProxy _systemSettings;
    private readonly ShortUrlService _shortUrlService;
    private readonly IDbContextFactory<KastaDbContext> _dbFactory;

    public LinkShortenerWebService(IServiceProvider services)
    {
        _db = services.GetRequiredService<KastaDbContext>();
        _userService = services.GetRequiredService<UserService>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _systemSettings = services.GetRequiredService<SystemSettingsProxy>();
        _shortUrlService = services.GetRequiredService<ShortUrlService>();
        _dbFactory = services.GetRequiredService<IDbContextFactory<KastaDbContext>>();
    }

    public async Task<DeleteShortenedLinkResult> Delete<T>(
        ILogger<T> logger,
        string value,
        T controller)
        where T : Controller
    {
        var user = await _userService.GetCurrentUser(controller.HttpContext);
        if (user == null)
        {
            return DeleteShortenedLinkResult.NotAuthorizedNotLoggedIn;
        }

        if (!_systemSettings.EnableLinkShortener)
        {
            return DeleteShortenedLinkResult.NotAuthorizedFeatureDisabled;
        }

        var valueLower = value?.Trim().ToLower();
        var model = await _db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == valueLower);
        model ??= await _db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ShortLink == value);

        if (model == null)
        {
            return DeleteShortenedLinkResult.NotFound;
        }
        
        if (model.CreatedByUserId != user.Id &&
            !await _userManager.IsInRoleAsync(user, RoleKind.Administrator))
        {
            return DeleteShortenedLinkResult.NotAuthorized;
        }

        await using var ctx = await _dbFactory.CreateDbContextAsync();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.ShortLinks.Where(e => e.Id == model.Id).ExecuteDeleteAsync();
            logger.LogInformation("User {UserEmail} ({UserId}) Deleted {Count} records (for Id={ModelId})",
                user.Email, user.Id,
                count, model.Id);
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            logger.LogError(ex, "Failed to delete record where Id={ModelId}", model.Id);
            throw;
        }

        return DeleteShortenedLinkResult.Success;
    }

    public enum DeleteShortenedLinkResult
    {
        Success,
        NotFound,
        NotAuthorizedFeatureDisabled,
        NotAuthorizedNotLoggedIn,
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
            return CreateShortenedLinkResult.InvalidUri;
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

            if (vanity.Length > 100)
            {
                return CreateShortenedLinkResult.VanityTooLong;
            }
        }

        ShortLinkModel result;
        using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            var shortLink = string.IsNullOrEmpty(vanity)
                ? _shortUrlService.GenerateForLinkShortener()
                : vanity;
            result = new ShortLinkModel
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = user.Id,
                Destination = url,
                ShortLink = shortLink,
                IsVanity = !string.IsNullOrEmpty(vanity)
            };

            await _db.ShortLinks.AddAsync(result);
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

        logger.LogInformation(
            "User {UserEmail} ({UserId}) created shortened link to \"{Destination}\" with code of {ShortLink} (IsVanity={IsVanity})",
            user.Email,
            user.Id,
            result.Destination,
            result.ShortLink,
            result.IsVanity);

        return CreateShortenedLinkResult.Success;
    }

    public enum CreateShortenedLinkResult
    {
        [Description("Successfully created link!")]
        Success,
        [Description("Vanity URL already exists")]
        VanityAlreadyExists,
        [Description("Vanity URL provided is too long! Must be less than 100 characters.")]
        VanityTooLong,
        [Description("Not Authorized - Please login")]
        NotAuthorizedNotLoggedIn,
        [Description("Not Authorized - You cannot create vanity URLs")]
        NotAuthorizedMissingVanityCreatorRole,
        [Description("Not Authorized - Link Shortener is disabled")]
        NotAuthorizedFeatureDisabled,
        [Description("Invalid URL")]
        InvalidUri
    }
}