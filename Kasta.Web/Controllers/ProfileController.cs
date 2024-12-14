using System.Text;
using System.Text.Json;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Controllers;

public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;

    public ProfileController(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
    }
    
    [AuthRequired]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            throw new InvalidOperationException($"Cannot view profile when user is null");
        }
        var settings = await _db.GetUserSettingsAsync(user);
        var keys = await _db.UserApiKeys.Where(e => e.UserId == user.Id).ToListAsync();
        var vm = new ProfileViewModel()
        {
            User = user,
            Settings = settings,
            ApiKeys = keys
        };
        return View("Index", vm);
    }

    [AuthRequired]
    public async Task<IActionResult> Save(
        [FromForm] UserProfileSaveParams data)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            throw new InvalidOperationException(
                $"User returned null from {typeof(UserManager<UserModel>)} (method: {nameof(_userManager.GetUserAsync)})");
        }


        using var ctx = _db.CreateSession();
        var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var settings = await ctx.GetUserSettingsAsync(currentUser);

            settings.ThemeName = data.ThemeName;
            settings.ShowFilePreviewInHome = data.ShowFilePreviewInHome;
            
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
            await ctx.SaveChangesAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        
        return new RedirectToActionResult(nameof(Index), "Profile", null);
    }

    [AuthRequired]
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

        var data = new Dictionary<string, object>()
        {
            {"DestinationType", "ImageUploader, TextUploader, FileUploader"},
            {"RequestURL", $"{FeatureFlags.Endpoint}/api/v1/File/Upload/Form"},
            {"FileFormName", "file"},
            {"Arguments", new Dictionary<string, object>()
            {
                {"filename", "$filename$"},
                {"token", apiKey.Token}
            }},
            {"URL", "$json:urlDetail$"},
            {"ThumbnailURL", "$json:url$"},
            {"DeletionURL", "$json:urlDelete$?token=" + apiKey.Token},
        };
        var fileContent = JsonSerializer.Serialize(data, new JsonSerializerOptions()
        {
            IncludeFields = true,
            WriteIndented = true
        });
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        using (var ctx = _db.CreateSession())
        {
            using (var trans = await ctx.Database.BeginTransactionAsync())
            {
                try
                {
                    await ctx.UserApiKeys.AddAsync(apiKey);
                    await ctx.SaveChangesAsync();
                    await trans.CommitAsync();
                }
                catch
                {
                    await trans.RollbackAsync();
                }
            }
        }
        return new FileStreamResult(ms, "application/json")
        {
            FileDownloadName = $"{currentUser.UserName}-ShareX.sxcu"
        };
    }

    [AuthRequired]
    [HttpGet]
    public async Task<IActionResult> GenerateRustgrabConfig()
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
        
        var data = new Dictionary<string, object>()
        {
            {"xbackbone_config", new Dictionary<string, object>()
            {
                {"token", apiKey.Token},
                {"url", $"{FeatureFlags.Endpoint}/api/v1/File/Upload/Form"}
            }}
        };
        var fileContent = JsonSerializer.Serialize(data, new JsonSerializerOptions()
        {
            IncludeFields = true,
            WriteIndented = true
        });
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        using (var ctx = _db.CreateSession())
        {
            using (var trans = ctx.Database.BeginTransaction())
            {
                try
                {
                    await ctx.UserApiKeys.AddAsync(apiKey);
                    trans.Commit();
                    ctx.SaveChanges();
                }
                catch
                {
                    trans.Rollback();
                }
            }
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

        using (var ctx = _db.CreateSession())
        {
            using var trans = ctx.Database.BeginTransaction();
            try
            {
                await ctx.UserApiKeys.Where(e => e.UserId == currentUser.Id).ExecuteDeleteAsync();
                trans.Commit();
                ctx.SaveChanges();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
        return new RedirectToActionResult(nameof(Index), "Profile", null);
    }
}