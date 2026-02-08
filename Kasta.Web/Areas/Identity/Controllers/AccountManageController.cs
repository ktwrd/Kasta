using CSharpFunctionalExtensions;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Areas.Identity.Models.AccountManage;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Areas.Identity.Controllers;

[Area("Identity")]
[Authorize]
[AuthRequired]
[Route("~/[area]/Account/Manage/_Components")]
public class AccountManageController : Controller
{
    private readonly ILogger<AccountManageController> _logger;
    private readonly UserManager<UserModel> _userManager;
    private readonly UserService _userService;
    private readonly ApplicationDbContext _db;

    public AccountManageController(IServiceProvider services, ILogger<AccountManageController> logger)
    {
        _logger = logger;
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _userService = services.GetRequiredService<UserService>();
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    private async Task<ApiKeysViewModel> GetApiKeysViewModel()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            throw new InvalidOperationException(
                "Controller can only be used by authenticated users, but I couldn't find the current user that's logged in!");
        }

        var keys = await _db.UserApiKeys
            .Include(e => e.CreatedByUser)
            .Include(e => e.User)
            .AsNoTracking()
            .Where(e => e.UserId == user.Id && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return new ApiKeysViewModel
        {
            CurrentUser = user,
            ApiKeys = keys
        };
    }
    
    [HttpGet]
    public async Task<IActionResult> GetApiKeysComponent()
    {
        var vm = await GetApiKeysViewModel();
        return PartialView("ApiKeys", vm);
    }

    [HttpPost("DeleteApiKey")]
    public async Task<IActionResult> DeleteApiKeysComponent([FromQuery] string apiKeyId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            throw new InvalidOperationException(
                "Controller can only be used by authenticated users, but I couldn't find the current user that's logged in!");
        }

        var key = await _db.UserApiKeys.FirstOrDefaultAsync(e => e.Id == apiKeyId && !e.IsDeleted);

        if (key == null)
        {
            var vm = await GetApiKeysViewModel();
            vm.Alert = new()
            {
                AlertContent = $"Api Key key doesn't exist: {apiKeyId}",
                AlertContentAsMarkdown = false,
                AlertType = "warning"
            };
            return PartialView("ApiKeys", vm);
        }
        else if (key.UserId != user.Id && !await _userManager.IsInRoleAsync(user, RoleKind.Administrator))
        {
            var vm = await GetApiKeysViewModel();
            vm.Alert = new()
            {
                AlertContent = $"You do not have permission to delete this Api Key: `{key.Id}`",
                AlertContentAsMarkdown = true,
                AlertType = "warning"
            };
            return PartialView("ApiKeys", vm);
        }

        try
        {
            var r = await _userService.DeleteApiKey(apiKeyId, Maybe.From(user), Maybe.From(HttpContext));
            if (r.IsFailure)
            {
                var vm = await GetApiKeysViewModel();
                vm.Alert = new()
                {
                    AlertContent = $"Failed to delete Api Key: `{key.Id}`\nReason: {r.Error.ToDescriptionString()}",
                    AlertContentAsMarkdown = true,
                    AlertType = "error"
                };
                return PartialView("ApiKeys", vm);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User {Username} ({UserId}) failed to delete Api Key {ApiKeyId}",
                user,
                user.Id,
                key.Id);
            var vm = await GetApiKeysViewModel();
            vm.Alert = new()
            {
                AlertContent = $"Failed to delete Api Key: `{key.Id}`\n" +
                               $"Type: `{ex.GetType().Namespace}.{ex.GetType().Name}`",
                AlertContentAsMarkdown = true,
                AlertType = "error"
            };
            return PartialView("ApiKeys", vm);
        }
        
        var finalModel = await GetApiKeysViewModel();
        finalModel.Alert = new()
        {
            AlertContent = $"Successfully deleted Api Key: `{key.Id}`",
            AlertContentAsMarkdown = true,
            AlertType = "success"
        };
        return PartialView("ApiKeys", finalModel);
    }
}