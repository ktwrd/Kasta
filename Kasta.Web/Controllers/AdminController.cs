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
    private readonly ILogger<AdminController> _logger;
    
    public AdminController(IServiceProvider services, ILogger<AdminController> logger)
        : base()
    {
        _logger = logger;
        _db = services.GetRequiredService<ApplicationDbContext>();
        _fileService = services.GetRequiredService<FileService>();
        _signInManager = services.GetRequiredService<SignInManager<UserModel>>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
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
}