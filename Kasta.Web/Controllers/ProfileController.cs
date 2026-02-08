using System.Text.Json;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Models;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using CreateUserApiKeyErrorKind = Kasta.Web.Services.UserService.CreateUserApiKeyErrorKind;

namespace Kasta.Web.Controllers;

[Authorize]
[AuthRequired(AllowApiKeyAuth = false)]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;
    private readonly UserService _userService;

    public ProfileController(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _userService = services.GetRequiredService<UserService>();
    }
    
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            throw new InvalidOperationException($"Cannot view profile when user is null");
        }
        var settings = await _db.GetUserSettingsAsync(user);
        var keys = await _db.UserApiKeys
            .AsNoTracking()
            .Where(e => e.UserId == user.Id && !e.IsDeleted)
            .ToListAsync();
        var vm = new ProfileViewModel()
        {
            User = user,
            Settings = settings,
            ApiKeys = keys
        };
        return View("Index", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Save(
        [FromForm] UserProfileSaveParams data)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            throw new InvalidOperationException(
                $"User returned null from {typeof(UserManager<UserModel>)} (method: {nameof(_userManager.GetUserAsync)})");
        }


        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var settings = await ctx.GetUserSettingsAsync(currentUser);

            settings.ThemeName = data.ThemeName;
            settings.ShowFilePreviewInHome = data.ShowFilePreviewInHome;
            
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        
        return new RedirectToActionResult(nameof(Index), "Profile", null);
    }

    [HttpGet]
    public async Task<IActionResult> GenerateShareXConfig()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            throw new InvalidOperationException(
                $"User returned null from {typeof(UserManager<UserModel>)} (method: {nameof(_userManager.GetUserAsync)})");
        }

        var apiKeyResult = await _userService.CreateApiKeyAsync(new()
        {
            UserId = currentUser.Id,
            Purpose = "ShareX Config (generated via profile)"
        });

        if (apiKeyResult.IsFailure)
        {
            switch (apiKeyResult.Error)
            {
                case CreateUserApiKeyErrorKind.NotLoggedIn:
                    Response.StatusCode = 403;
                    return View("NotAuthorized");
                case CreateUserApiKeyErrorKind.CannotCreateTokenForOtherUsers:
                    Response.StatusCode = 403;
                    return View("NotAuthorized", new NotAuthorizedViewModel()
                    {
                        Message = "You do not have permission to create Api Keys for other users."
                    });
                case CreateUserApiKeyErrorKind.UserNotFound:
                    Response.StatusCode = 404;
                    return View("NotFound", new NotFoundViewModel()
                    {
                        Message = "Could not find User: " + currentUser.Id
                    });
                default:
                    throw new NotImplementedException($"{apiKeyResult.Error}");
            }
        }

        var apiKey = apiKeyResult.Value;

        var data = new ShareXConfigModel()
        {
            DestinationType = "ImageUploader, TextUploader, FileUploader",
            RequestUrl = $"{FeatureFlags.Endpoint}/api/v1/File/Upload/Form",
            FileFormName = "file",
            Arguments = new()
            {
                { "filename", "$filename$" },
                { "token", apiKey.Token }
            },
            Url = "$json:urlDetail$",
            ThumbnailUrl = "$json:url$",
            DeletionUrl = "$json:urlDelete$?token=" + apiKey.Token
        };

        var ms = new MemoryStream();
        await JsonSerializer.SerializeAsync(ms, data, JsonSerializerOptions);
        ms.Seek(0, SeekOrigin.Begin);
        return new FileStreamResult(ms, "application/json")
        {
            FileDownloadName = $"{currentUser.UserName}-ShareX.sxcu"
        };
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true
    };

    [HttpGet]
    public async Task<IActionResult> GenerateRustGrabConfig()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            throw new InvalidOperationException(
                $"User returned null from {typeof(UserManager<UserModel>)} (method: {nameof(_userManager.GetUserAsync)})");
        }

        
        var apiKeyResult = await _userService.CreateApiKeyAsync(new()
        {
            UserId = currentUser.Id,
            Purpose = "rustgrab Config (generated via profile)"
        });

        if (apiKeyResult.IsFailure)
        {
            switch (apiKeyResult.Error)
            {
                case CreateUserApiKeyErrorKind.NotLoggedIn:
                    Response.StatusCode = 403;
                    return View("NotAuthorized");
                case CreateUserApiKeyErrorKind.CannotCreateTokenForOtherUsers:
                    Response.StatusCode = 403;
                    return View("NotAuthorized", new NotAuthorizedViewModel()
                    {
                        Message = "You do not have permission to create Api Keys for other users."
                    });
                case CreateUserApiKeyErrorKind.UserNotFound:
                    Response.StatusCode = 404;
                    return View("NotFound", new NotFoundViewModel()
                    {
                        Message = "Could not find User: " + currentUser.Id
                    });
                default:
                    throw new NotImplementedException($"{apiKeyResult.Error}");
            }
        }

        var apiKey = apiKeyResult.Value;
        
        // TODO create a new class instead of raw-dogging it with a dict
        var data = new Dictionary<string, object>()
        {
            {"xbackbone_config", new Dictionary<string, object>()
            {
                {"token", apiKey.Token},
                {"url", $"{FeatureFlags.Endpoint}/api/v1/File/Upload/Form"}
            }}
        };
        var ms = new MemoryStream();
        await JsonSerializer.SerializeAsync(ms, data, JsonSerializerOptions);
        return new FileStreamResult(ms, "application/json")
        {
            FileDownloadName = $"{currentUser.UserName}-rustgrab-partial.json"
        };
    }

    [HttpGet]
    public async Task<IActionResult> DeleteAllApiKeys()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            throw new InvalidOperationException(
                $"User returned null from {typeof(UserManager<UserModel>)} (method: {nameof(_userManager.GetUserAsync)})");
        }

        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await ctx.UserApiKeys.Where(e => e.UserId == currentUser.Id).ExecuteDeleteAsync();
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
        return new RedirectToActionResult(nameof(Index), "Profile", null);
    }
}