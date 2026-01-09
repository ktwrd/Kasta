using System.Text;
using System.Text.Json;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Controllers;

[AuthRequired]
[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;

    public ProfileController(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
    }
    
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
            .Where(e => e.UserId == user.Id)
            .ToListAsync();
        var vm = new ProfileViewModel()
        {
            User = user,
            Settings = settings,
            ApiKeys = keys
        };
        return View("Index", vm);
    }

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

        var apiKey = new UserApiKeyModel()
        {
            UserId = currentUser.Id,
            CreatedByUserId = currentUser.Id,
            Purpose = "Generate ShareX Config (from Profile)"
        };

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
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await ctx.UserApiKeys.AddAsync(apiKey);
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
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

        var apiKey = new UserApiKeyModel()
        {
            UserId = currentUser.Id,
            CreatedByUserId = currentUser.Id,
            Purpose = "Generate rustgrab Config (from Profile)"
        };
        
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
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await ctx.UserApiKeys.AddAsync(apiKey);
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
        return new FileStreamResult(ms, "application/json")
        {
            FileDownloadName = $"{currentUser.UserName}-rustgrab-partial.json"
        };
    }

    [AuthRequired]
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