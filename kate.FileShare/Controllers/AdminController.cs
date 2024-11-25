using kate.FileShare.Data;
using kate.FileShare.Data.Models;
using kate.FileShare.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace kate.FileShare.Controllers;

[Route("~/Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SignInManager<UserModel> _signInManager;
    private readonly UserManager<UserModel> _userManager;
    
    public AdminController(IServiceProvider services)
        : base()
    {
        _dbContext = services.GetRequiredService<ApplicationDbContext>();
        _signInManager = services.GetRequiredService<SignInManager<UserModel>>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
    }

    [AuthRequired]
    [HttpGet]
    public IActionResult Home()
    {
        var user = _userManager.GetUserAsync(User).Result;
        if (user == null || !user.IsAdmin)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }
        var model = new AdminIndexViewModel();

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
    public async Task<IActionResult> SaveSystemSettings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsAdmin)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }
        throw new NotImplementedException();
    }
}