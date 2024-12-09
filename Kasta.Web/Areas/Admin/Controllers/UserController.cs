using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Areas.Admin.Models.User;
using Kasta.Web.Helpers;
using Kasta.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Areas.Admin.Controllers;

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
        foreach (var user in vm.Users)
        {
            var files = await _db.Files.Where(e => e.CreatedByUserId == user.Id).Select(e => e.Id).ToListAsync();
            var previewFileCount = await _db.FilePreviews.Where(e => files.Contains(e.Id)).LongCountAsync();
            vm.UserPreviewFileCount[user.Id] = previewFileCount;
            vm.UserFileCount[user.Id] = files.LongCount();
        }
        vm.IsLastPage = lastPage;

        return View("UserList", vm);
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

        return View("Details", targetUser);
    }

    [HttpPost("{userId}/Edit")]
    public async Task<IActionResult> EditUserPostForm(
        string userId,
        [FromForm] EditUserContract body)
    {
        if (!await _db.UserExistsAsync(userId))
        {
            Response.StatusCode = 404;
            return View("NotFound", new NotFoundViewModel()
            {
                Message = $"Could not find User with Id of `{userId}`"
            });
        }

        using (var ctx = _db.CreateSession())
        {
            var trans = ctx.Database.BeginTransaction();

            try
            {
                var user = await ctx.GetUserAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"WTF??? Checked if the user ({userId}) exists outside of new context, but it doesn't??");
                }

                long? storageParse = body.EnableStorageQuota || string.IsNullOrEmpty(body.StorageQuotaValue)
                    ? null
                    : SizeHelper.ParseToByteCount(body.StorageQuotaValue!);
                long? uploadParse= body.EnableUploadLimit || string.IsNullOrEmpty(body.UploadLimitValue)
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
                            e.SetProperty(e => e.MaxFileSize, uploadParse)
                             .SetProperty(e => e.MaxStorage, storageParse)
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

            return new RedirectToActionResult("GetUser", "User", new {
                userId = userId
            });
        }
    }
}