using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using kate.FileShare.Models;
using kate.FileShare.Services;
using kate.FileShare.Data.Models;
using Microsoft.AspNetCore.Identity;
using kate.FileShare.Data;
using kate.FileShare.Helpers;
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

    public IActionResult Index([FromQuery] string? search = null, [FromQuery] int? page = 1)
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            var userModel = _userManager.GetUserAsync(User).Result;
            if (userModel == null)
            {
                throw new InvalidOperationException($"User Model cannot be null when authenticated");
            }
            var viewModel = new FileListViewModel()
            {
                SearchQuery = string.IsNullOrEmpty(search) ? null : search
            };
            if (page.HasValue && page.Value >= 1)
            {
                viewModel.Page = page.Value;
            }
            var query = _db
                .SearchFiles(viewModel.SearchQuery, userModel.Id)
                .OrderByDescending(v => v.CreatedAt)
                .Include(fileModel => fileModel.Preview);
            viewModel.Files = _db.Paginate(query, viewModel.Page, 25, out bool lastPage);
            viewModel.IsLastPage = lastPage;

            var systemSettings = _db.GetSystemSettings();
            var userQuota = _db.UserLimits.Where(e => e.UserId == userModel.Id).FirstOrDefault();
            viewModel.SpaceUsed = SizeHelper.BytesToString(userQuota?.SpaceUsed ?? 0);
            if (systemSettings.EnableQuota)
            {
                if (userQuota?.MaxStorage >= 0)
                {
                    viewModel.SpaceAvailable = SizeHelper.BytesToString(Math.Max((userQuota.MaxStorage - userQuota.SpaceUsed) ?? 0, 0));
                }
                else if (systemSettings?.DefaultStorageQuotaReal >= 0)
                {
                    if (userQuota == null)
                    {
                        viewModel.SpaceAvailable = SizeHelper.BytesToString(systemSettings.DefaultStorageQuotaReal ?? 0);
                    }
                    else
                    {
                        viewModel.SpaceAvailable = SizeHelper.BytesToString(Math.Max((systemSettings?.DefaultStorageQuotaReal - userQuota.SpaceUsed) ?? 0, 0));
                    }
                }
            }

            return View("FileList", viewModel);
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
    public async Task<IActionResult> UploadPost(IFormFile file, [FromQuery] bool returnJson = false)
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
                var vm = new NotAuthorizedViewModel()
                {
                    Message = $"You don't have enough space to upload file (file size: {SizeHelper.BytesToString(file.Length)}, storage: {SizeHelper.BytesToString(spaceAllocated)})",
                };
                if (returnJson)
                {
                    return Json(vm);
                }
                return View("NotAuthorized", vm);
            }

            var maxFileSize = userLimit?.MaxFileSize ?? systemSettings.DefaultUploadQuotaReal ?? long.MaxValue;
            if (file.Length > maxFileSize)
            {
                HttpContext.Response.StatusCode = 413;
                var vm = new NotAuthorizedViewModel()
                {
                    Message = $"Provided file exceeds maximum file size ({SizeHelper.BytesToString(maxFileSize)})"
                };
                if (returnJson)
                {
                    return Json(vm);
                }
                return View("NotAuthorized", vm);
            }
        }

        FileModel resultFile;
        using (var stream = file.OpenReadStream())
        {
            resultFile = await _uploadService.UploadBasicAsync(user, stream, file.FileName, file.Length);
        }
        if (returnJson)
        {
            return Json(new Dictionary<string, object>()
            {
                { "message", "OK" },
                { "url", $"/f/{resultFile.ShortUrl}" }
            });
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