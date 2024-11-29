using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Models;
using Kasta.Web.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models.Admin;
using Microsoft.EntityFrameworkCore;
using Kasta.Web.Services;

namespace Kasta.Web.Controllers;

[Route("~/Admin")]
[AdminRequired]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly FileService _fileService;
    private readonly SignInManager<UserModel> _signInManager;
    private readonly UserManager<UserModel> _userManager;
    
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
        vm.IsLastPage = lastPage;

        return View("UserList", vm);
    }
}