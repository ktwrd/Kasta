using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Models;
using Kasta.Web.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models.Admin;
using Microsoft.EntityFrameworkCore;
using Kasta.Web.Services;
using NLog;

namespace Kasta.Web.Controllers;

[Route("~/Admin")]
[AdminRequired]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly FileService _fileService;
    private readonly SignInManager<UserModel> _signInManager;
    private readonly UserManager<UserModel> _userManager;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    
    public AdminController(IServiceProvider services)
        : base()
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _fileService = services.GetRequiredService<FileService>();
        _signInManager = services.GetRequiredService<SignInManager<UserModel>>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
    }

    [AuthRequired]
    public IActionResult Index()
    {
        var user = _userManager.GetUserAsync(User).Result;
        if (user == null || !user.IsAdmin)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }
        var model = new AdminIndexViewModel();
        model.SystemSettings = _db.GetSystemSettings();
        model.UserCount = _db.Users.Count();

        var spaceUsedValue = _db.UserLimits.Select(e => e.SpaceUsed).Sum();
        model.TotalSpaceUsed = SizeHelper.BytesToString(spaceUsedValue);

        var previewSpaceUsedValue = _db.UserLimits.Select(e => e.PreviewSpaceUsed).Sum();
        model.TotalPreviewSpaceUsed = SizeHelper.BytesToString(previewSpaceUsedValue);

        model.OrphanFileCount = _db.Files.Where(e => e.CreatedByUser == null).Include(e => e.CreatedByUser).Count();

        model.FileCount = _db.Files.Count();

        return View("Index", model);
    }

    [AuthRequired]
    [HttpGet("Audit")]
    public async Task<IActionResult> AuditIndex()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsAdmin)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }

        return View();
    }

    [AuthRequired]
    [HttpGet("System/RecalculateStorage")]
    public async Task<IActionResult> RecalculateStorage()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }

        var taskList = new List<Task>();
        foreach (var user in _db.Users.ToList())
        {
            taskList.Add(new Task(delegate
            {
                _fileService.RecalculateSpaceUsed(user).Wait();
            }));
        }
        foreach (var t in taskList)
            t.Start();
        await Task.WhenAll(taskList);

        return new RedirectToActionResult("Index", "Admin", null);
    }

    [AuthRequired]
    [HttpPost("System/Save")]
    public async Task<IActionResult> SaveSystemSettings(
        [FromForm] SystemSettingsParams data)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsAdmin)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }

        using var ctx = _db.CreateSession();
        using var transaction = await ctx.Database.BeginTransactionAsync();

        try
        {
            data.InsertOrUpdate(ctx);
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new RedirectToActionResult(nameof(Index), "Admin", null);
    }

    [AuthRequired]
    [HttpGet("Users")]
    public async Task<IActionResult> UserList(
        [FromQuery] int page = 1)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }

        if (page < 1)
            page = 1;
        var vm = new AdminUserListViewModel()
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

    [AuthRequired]
    [HttpGet("EditUser")]
    public async Task<IActionResult> EditUserPage([FromQuery] string userId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }

        var targetUser = await _db.GetUserAsync(userId);
        if (targetUser == null)
        {
            Response.StatusCode = 404;
            return View("NotFound", new NotFoundViewModel()
            {
                Message = $"Could not find User with Id of `{userId}`"
            });
        }

        return View("EditUser", targetUser);
    }

    [AuthRequired]
    [HttpPost("EditUser")]
    public async Task<IActionResult> EditUserPost(
        [FromForm] string userId,
        [FromForm] bool isAdmin,
        [FromForm] bool enableStorageQuota,
        [FromForm] string? storageQuotaValue,
        [FromForm] bool enableUploadLimit,
        [FromForm] string? uploadLimitValue)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }

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

                long? storageParse = enableStorageQuota || string.IsNullOrEmpty(storageQuotaValue)
                    ? null
                    : SizeHelper.ParseToByteCount(storageQuotaValue!);
                long? uploadParse= enableUploadLimit || string.IsNullOrEmpty(uploadLimitValue)
                    ? null
                    : SizeHelper.ParseToByteCount(uploadLimitValue!);

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
                user.IsAdmin = isAdmin;
                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }

            return new RedirectToActionResult("UserList", "Admin", null);
        }
    }
}