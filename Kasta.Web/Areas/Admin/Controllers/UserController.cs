using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared.Helpers;
using Kasta.Web.Areas.Admin.Models.User;
using Kasta.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("~/Admin/[controller]")]
[Authorize(Roles = $"{RoleKind.Administrator}, {RoleKind.UserAdmin}")]
public class UserController : Controller
{
    private readonly UserManager<UserModel> _userManager;
    private readonly ApplicationDbContext _db;
    public UserController(IServiceProvider services)
    {
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    [HttpGet("List")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }

        if (page < 1)
            page = 1;
        var vm = new UserListViewModel()
        {
            Page = page
        };
        vm.SystemSettings = _db.GetSystemSettings();
        vm.Users = _db.Paginate(
            _db.Users.OrderBy(e => e.IsAdmin).Include(e => e.Limit),
            vm.Page,
            50,
            out var lastPage);
        var allFiles = await _db.Files.AsNoTracking().Select(e => new { e.Id, e.CreatedByUserId }).ToListAsync();
        var allFileIds = allFiles.Select(e => e.Id).ToList();
        var allPreviewFileIds = await _db.FilePreviews.AsNoTracking()
            .Where(e => allFileIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();
        foreach (var user in vm.Users)
        {
            var ourFileIds = allFiles.Where(e => e.CreatedByUserId == user.Id).Select(e => e.Id).ToList();
            var previewFileCount = allPreviewFileIds.Where(e => ourFileIds.Contains(e)).LongCount();
            vm.UserPreviewFileCount[user.Id] = previewFileCount;
            vm.UserFileCount[user.Id] = ourFileIds.LongCount();
        }
        vm.IsLastPage = lastPage;

        return View("List", vm);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var targetUser = await _db.GetUserAsync(userId);
        if (targetUser == null)
        {
            Response.StatusCode = 404;
            return View("NotFound", new NotFoundViewModel()
            {
                Message = $"Could not find User with Id of `{userId}`"
            });
        }

        var vm = new UserDetailsViewModel()
        {
            User = targetUser
        };

        var roles = await _db.Roles
            .AsNoTracking()
            .ToListAsync();
        vm.Roles = roles.ToDictionary(e => e.Id, e => e);

        var userRoles = await _db.UserRoles
            .AsNoTracking()
            .Where(e => e.UserId == targetUser.Id)
            .ToListAsync();
        vm.UserRoles = userRoles.ToDictionary(e => e.RoleId, e => e.UserId);

        return View("Details", vm);
    }

    [HttpGet("{userId}/Edit/Roles/Component")]
    public async Task<IActionResult> EditUserRolesComponent(
        string userId)
    {
        if (!await _db.UserExistsAsync(userId))
        {
            var userIdSanitized = userId.Replace("<", "&lt;").Replace(">", "&gt;");
            Response.StatusCode = 200;
            return Content($"<div class=\"alert alert-danger\" role=\"alert\">Could not find User with Id <code>{userIdSanitized}</code></div>");
        }
        
        var vm = await GetRoleDetailsComponentViewModel(userId);
        return PartialView("RoleDetailsComponent", vm);
    }

    [HttpPost("{userId}/Edit/Roles/Component")]
    public async Task<IActionResult> EditUserRolesComponentPost(
        string userId,
        [FromForm] Dictionary<string, bool> userRoles)
    {
        if (!await _db.UserExistsAsync(userId))
        {
            var userIdSanitized = userId.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\\", "");
            Response.StatusCode = 200;
            return Content($"<div class=\"alert alert-danger\" role=\"alert\">Could not find User with Id <code>{userIdSanitized}</code></div>");
        }

        await using (var ctx = _db.CreateSession())
        {
            await using var trans = await ctx.Database.BeginTransactionAsync();

            try
            {
                var existingRoles = await ctx.UserRoles
                    .Where(e => e.UserId == userId)
                    .Select(e => e.RoleId)
                    .ToListAsync();

                var roleIdRemoveList = new List<string>();
                var roleIdAddList = new List<string>();

                foreach (var (targetRoleId, targetState) in userRoles)
                {
                    if (targetState)
                    {
                        roleIdAddList.Add(targetRoleId);
                    }
                    else
                    {
                        roleIdRemoveList.Add(targetRoleId);
                    }
                }

                foreach (var targetRoleId in existingRoles)
                {
                    if (!roleIdRemoveList.Contains(targetRoleId) && !roleIdAddList.Contains(targetRoleId))
                    {
                        roleIdRemoveList.Add(targetRoleId);
                    }
                }
                roleIdAddList = roleIdAddList.Where(e => existingRoles.Contains(e) == false).ToList();
                var targetRoleIdAddList = await ctx.Roles
                    .AsNoTracking()
                    .Where(e => roleIdAddList.Contains(e.Id))
                    .Select(e => e.Id)
                    .ToListAsync();

                await ctx.UserRoles
                    .Where(e => e.UserId == userId).Where(e => roleIdRemoveList.Contains(e.RoleId))
                    .ExecuteDeleteAsync();

                var existingUserRoleIds = await ctx.UserRoles
                    .AsNoTracking()
                    .Where(e => e.UserId == userId)
                    .Select(e => e.RoleId)
                    .ToListAsync();
                foreach (var roleId in targetRoleIdAddList.Where(e => !existingUserRoleIds.Contains(e)))
                {
                    var m = new IdentityUserRole<string>()
                    {
                        UserId = userId,
                        RoleId = roleId
                    };
                    await ctx.UserRoles.AddAsync(m);
                }
                /*foreach (var roleId in targetRoleIdAddList)
                {
                    var condition = await ctx.UserRoles
                        .Where(e => e.UserId == userId && e.RoleId == roleId)
                        .AnyAsync();
                    if (!condition)
                    {
                        var m = new IdentityUserRole<string>()
                        {
                            UserId = userId,
                            RoleId = roleId
                        };
                        await ctx.UserRoles.AddAsync(m);
                    }
                }*/

                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        var vm = await GetRoleDetailsComponentViewModel(userId);
        vm.AlertType = "success";
        vm.AlertContent = "Saved Roles";
        return PartialView("RoleDetailsComponent", vm);
    }

    private async Task<RoleDetailsComponentViewModel> GetRoleDetailsComponentViewModel(string userId)
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .Where(e => e.Name != null)
            .ToListAsync();
        var userRoles = await _db.UserRoles
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => e.RoleId)
            .ToListAsync();
        var vm = new RoleDetailsComponentViewModel()
        {
            UserId = userId,
            UserRoleIds = userRoles,
            Roles = roles.ToDictionary(e => e.Id, e => e.Name ?? e.Id)
        };
        return vm;
    }

    [HttpGet("{userId}/Edit/Component")]
    public async Task<IActionResult> EditUserLimitComponent(
        string userId)
    {
        if (!await _db.UserExistsAsync(userId))
        {
            var userIdSanitized = userId.Replace("<", "&lt;").Replace(">", "&gt;");
            Response.StatusCode = 200;
            return Content($"<div class=\"alert alert-danger\" role=\"alert\">Could not find User with Id <code>{userIdSanitized}</code></div>");
        }

        var vm = await GetEditDetailsComponentViewModel(userId);
        return PartialView("EditDetailsComponent", vm);
    }

    [HttpPost("{userId}/Edit/Component")]
    public async Task<IActionResult> EditUserLimitComponentPost(
        string userId,
        [FromForm] EditUserContract body)
    {
        if (!await _db.UserExistsAsync(userId))
        {
            var userIdSanitized = userId.Replace("<", "&lt;").Replace(">", "&gt;");
            Response.StatusCode = 200;
            return Content($"<div class=\"alert alert-danger\" role=\"alert\">Could not find User with Id <code>{userIdSanitized}</code></div>");
        }

        await using (var ctx = _db.CreateSession())
        {
            await using var trans = await ctx.Database.BeginTransactionAsync();

            try
            {
                var user = await ctx.GetUserAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"WTF??? Checked if the user ({userId}) exists outside of new context, but it doesn't??");
                }

                var storageParse = body.EnableStorageQuota || string.IsNullOrEmpty(body.StorageQuotaValue)
                    ? null
                    : SizeHelper.ParseToByteCount(body.StorageQuotaValue!);
                var uploadParse= body.EnableUploadLimit || string.IsNullOrEmpty(body.UploadLimitValue)
                    ? null
                    : SizeHelper.ParseToByteCount(body.UploadLimitValue!);

                if (user.Limit == null)
                {
                    var limit = new UserLimitModel()
                    {
                        UserId = user.Id,
                        MaxFileSize = uploadParse,
                        MaxStorage = storageParse
                    };
                    await ctx.UserLimits.AddAsync(limit);
                }
                else
                {
                    await ctx.UserLimits.Where(e => e.UserId == user.Id)
                        .ExecuteUpdateAsync(e =>
                            e.SetProperty(p => p.MaxFileSize, uploadParse)
                             .SetProperty(p => p.MaxStorage, storageParse)
                        );
                }

                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }
        
        var vm = await GetEditDetailsComponentViewModel(userId);
        vm.AlertType = "success";
        vm.AlertContent = "Saved Storage Limit";
        return PartialView("EditDetailsComponent", vm);
    }

    private async Task<EditDetailsComponentViewModel> GetEditDetailsComponentViewModel(string userId)
    {
        var limit = await _db.UserLimits
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId);
        var vm = new EditDetailsComponentViewModel()
        {
            UserId = userId,
            Limit = limit
        };
        return vm;
    }
}