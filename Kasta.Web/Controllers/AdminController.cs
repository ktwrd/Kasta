using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Models;
using Kasta.Web.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.Controllers;

[Route("~/Admin")]
[AdminRequired]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly SignInManager<UserModel> _signInManager;
    private readonly UserManager<UserModel> _userManager;
    
    public AdminController(IServiceProvider services)
        : base()
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
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
    [HttpPost("Settings/Save")]
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
}