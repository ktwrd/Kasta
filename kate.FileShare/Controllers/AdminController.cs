using kate.FileShare.Data;
using kate.FileShare.Data.Models;
using kate.FileShare.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace kate.FileShare.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SignInManager<UserModel> _signInManager;
    
    public AdminController(IServiceProvider services)
        : base()
    {
        _dbContext = services.GetRequiredService<ApplicationDbContext>();
        _signInManager = services.GetRequiredService<SignInManager<UserModel>>();
    }

    [HttpGet("/Admin")]
    public async Task<IActionResult> Index()
    {
        if (!_signInManager.IsSignedIn(User))
        {
            return RedirectToPage("Index", "Home");
        }
        var model = new AdminIndexViewModel();

        return View("Index", model);
    }

    [HttpPost("/Admin/Settings/Save")]
    public async Task<IActionResult> SaveSystemSettings()
    {
        throw new NotImplementedException();
    }
}