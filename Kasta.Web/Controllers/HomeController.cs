using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models;
using Kasta.Web.Services;
using Kasta.Data.Models;
using Microsoft.AspNetCore.Identity;
using Kasta.Data;
using Kasta.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Kasta.Web.Controllers;

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
    [HttpGet("Links")]
    [Authorize]
    public async Task<IActionResult> LinkList([FromQuery] int? page = 1)
    {
        var systemSettings = _db.GetSystemSettings();
        if (systemSettings.EnableLinkShortener == false)
        {
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "Link Shortener is disabled"
            });
        }
        var vm = new LinkListViewModel();
        if (page.HasValue && page.Value >= 1)
        {
            vm.Page = page.Value;
        }
        var query = _db.ShortLinks.OrderByDescending(v => v.CreatedAt);
        (vm.Links, vm.IsLastPage) = await _db.PaginateAsync(query, vm.Page, 50);
        return View("LinkList", vm);
    }

    [HttpGet("Links/Delete")]
    [Authorize]
    public async Task<IActionResult> DeleteShortenedLink([FromQuery] string value, [FromQuery] string? returnUrl = null)
    {
        var systemSettings = _db.GetSystemSettings();
        if (systemSettings.EnableLinkShortener == false)
        {
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "Link Shortener is disabled"
            });
        }
        
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotAuthorized");
        }

        var model = await _db.ShortLinks.Where(e => e.Id == value).FirstOrDefaultAsync();
        model ??= await _db.ShortLinks.Where(e => e.ShortLink == value).FirstOrDefaultAsync();

        if (model == null)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotFound");
        }

        if (model.CreatedByUserId != user.Id)
        {
            if (await _userManager.IsInRoleAsync(user, RoleKind.Administrator) == false)
            {
                HttpContext.Response.StatusCode = 403;
                return View("NotAuthorized");
            }
        }

        using (var ctx = _db.CreateSession())
        {
            var trans = await ctx.Database.BeginTransactionAsync();
            try
            {
                await _db.ShortLinks.Where(e => e.Id == model.Id).ExecuteDeleteAsync();
                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }
        
        if (!string.IsNullOrEmpty(returnUrl))
        {
            return new RedirectResult(returnUrl);
        }
        return new RedirectToActionResult(nameof(LinkList), "Home", null);
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
    public async Task<IActionResult> DeleteFile(string id, [FromQuery] string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            throw new InvalidOperationException($"Unable to fetch User model even though user is logged in?");
        }
        var file = await _db.GetFileAsync(id);
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
        if (!string.IsNullOrEmpty(returnUrl))
        {
            return new RedirectResult(returnUrl);
        }
        return new RedirectToActionResult(nameof(Index), "Home", null);
    }

    [AuthRequired]
    [HttpGet("~/FilePublic/{id}")]
    public async Task<IActionResult> ChangeFilePublicState(
        string id,
        [FromQuery] bool value,
        [FromQuery] string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            throw new InvalidOperationException($"Unable to fetch User model even though user is logged in?");
        }
        var file = await _db.GetFileAsync(id);
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

        using (var ctx = _db.CreateSession())
        {
            using var trans = await ctx.Database.BeginTransactionAsync();
            try
            {
                await ctx.Files.Where(e => e.Id == file.Id)
                    .ExecuteUpdateAsync(e  => e.SetProperty(p => p.Public, value));
                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }
        
        if (!string.IsNullOrEmpty(returnUrl))
        {
            return new RedirectResult(returnUrl);
        }
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