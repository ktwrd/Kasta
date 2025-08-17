using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Areas.Gallery.Models.GalleryHome;
using Kasta.Web.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Areas.Gallery.Controllers;

[Area("Gallery")]
[Route("~/Gallery/List")]
[AuthRequired]
public class GalleryHomeController : Controller, IGalleryController
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;
    private readonly ILogger<GalleryDetailsController> _logger;
    
    #region IGalleryController
    public ApplicationDbContext Database => _db;
    public UserManager<UserModel> UserManager => _userManager;
    #endregion IGalleryController
    
    public GalleryHomeController(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _logger = services.GetRequiredService<ILogger<GalleryDetailsController>>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
    }
    
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] int? page = 1)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotAuthorized");
        }
        
        var vm = new ListViewModel();
        if (page is >= 1)
        {
            vm.Page = page.Value;
        }
        var galleryQuery = _db.Galleries
            .Where(e => e.CreatedByUserId == user.Id)
            .OrderByDescending(v => v.CreatedAt);
        vm.Galleries = _db.Paginate(galleryQuery, vm.Page, 25, out bool lastPage, out var lastPageNumber);
        vm.TotalPageCount = lastPageNumber;
        vm.IsLastPage = lastPage;
        var systemSettings = _db.GetSystemSettings();
        var userQuota = _db.UserLimits
            .AsNoTracking()
            .FirstOrDefault(e => e.UserId == user.Id);
        vm.SpaceUsed = SizeHelper.BytesToString(userQuota?.SpaceUsed ?? 0);
        if (systemSettings.EnableQuota)
        {
            if (userQuota?.MaxStorage >= 0)
            {
                vm.SpaceAvailable = SizeHelper.BytesToString(Math.Max((userQuota.MaxStorage - userQuota.SpaceUsed) ?? 0, 0));
            }
            else if (systemSettings?.DefaultStorageQuotaReal >= 0)
            {
                if (userQuota == null)
                {
                    vm.SpaceAvailable = SizeHelper.BytesToString(systemSettings.DefaultStorageQuotaReal ?? 0);
                }
                else
                {
                    vm.SpaceAvailable = SizeHelper.BytesToString(Math.Max((systemSettings?.DefaultStorageQuotaReal - userQuota.SpaceUsed) ?? 0, 0));
                }
            }
        }
        
        return View("Index", vm);
    }
    
    [HttpGet("Delete")]
    public async Task<IActionResult> DeleteGallery(string id)
    {
        return RedirectToAction("Delete", "GalleryDetails", new { Area = "Gallery", galleryId = id});
    }
}