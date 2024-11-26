using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using kate.FileShare.Models;
using kate.FileShare.Services;
using kate.FileShare.Data.Models;
using Microsoft.AspNetCore.Identity;
using kate.FileShare.Data;
using Microsoft.EntityFrameworkCore;

namespace kate.FileShare.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<UserModel> _userManager;
    private readonly UploadService _uploadService;
    private readonly ApplicationDbContext _db;
    private readonly FileService _fileService;

    public HomeController(
        ILogger<HomeController> logger,
        UserManager<UserModel> userManager,
        UploadService uploadService,
        ApplicationDbContext db,
        FileService fileService)
    {
        _logger = logger;
        _userManager = userManager;
        _uploadService = uploadService;
        _db = db;
        _fileService = fileService;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return View("FileList");
        }
        else
        {
            return new RedirectResult("/Identity/Account/Login", false);
        }
    }

    [AuthRequired]
    [HttpGet("~/Upload")]
    public IActionResult Upload()
    {
        return View();
    }

    [AuthRequired]
    [HttpPost("~/Upload")]
    public async Task<IActionResult> UploadPost(IFormFile file)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            throw new InvalidOperationException($"Unable to fetch User model even though user is logged in?");
        }
        
        var userLimit = await _db.UserLimits.Where(e => e.UserId == user.Id).FirstOrDefaultAsync();
        var systemSettings = _db.GetSystemSettings();
        if (systemSettings.EnableQuota)
        {
            long spaceUsed = userLimit?.SpaceUsed ?? 0;
            var spaceAllocated = userLimit?.MaxStorage ?? systemSettings.DefaultStorageQuotaReal ?? 0;
            if ((spaceUsed + file.Length) > spaceAllocated)
            {
                HttpContext.Response.StatusCode = 401;
                return View(
                    "NotAuthorized", new NotAuthorizedViewModel()
                    {
                        Message =
                            $"You don't have enough space to upload file (file size: {SizeHelper.BytesToString(file.Length)}, storage: {SizeHelper.BytesToString(spaceAllocated)})",
                    });
            }

            var maxFileSize = userLimit?.MaxFileSize ?? systemSettings.DefaultUploadQuotaReal ?? long.MaxValue;
            if (file.Length > maxFileSize)
            {
                HttpContext.Response.StatusCode = 400;
                return View(
                    "NotAuthorized", new NotAuthorizedViewModel()
                    {
                        Message = $"Provided file exceeds maximum file size ({SizeHelper.BytesToString(maxFileSize)})"
                    });
            }
        }

        using (var stream = file.OpenReadStream())
        {
            await _uploadService.UploadBasicAsync(user, stream, file.FileName, file.Length);
        }
        return new RedirectToActionResult(nameof(Index), "Home", null);
    }

    [AuthRequired]
    [HttpGet("~/Delete/{id}")]
    public async Task<IActionResult> DeleteFile(string id)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            throw new InvalidOperationException($"Unable to fetch User model even though user is logged in?");
        }
        var file = await _db.Files.Where(v => v.Id == id).Include(e => e.CreatedByUser).FirstOrDefaultAsync();
        file ??= await _db.Files.Where(v => v.ShortUrl == id).Include(e => e.CreatedByUser).FirstOrDefaultAsync();
        if (file == null)
        {
            return View("NotFound");
        }
        if (file.CreatedByUserId != user.Id)
        {
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = $"You did not create this file."
            });
        }

        await _fileService.DeleteFile(user, file);
        return new RedirectToActionResult(nameof(Index), "Home", null);
    }

    [Route("~/Error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
    }
}