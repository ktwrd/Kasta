using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Data.Models.Gallery;
using Kasta.Web.Areas.Gallery.Models.Details;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Areas.Gallery.Controllers;

[Route("~/Gallery/Details")]
public class DetailsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;
    private readonly ILogger<DetailsController> _logger;

    public DetailsController(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _logger = services.GetRequiredService<ILogger<DetailsController>>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
    }

    private async Task<bool> CanAccessGallery(UserModel? user, GalleryModel gallery)
    {
        if (gallery.Public) return true;
        if (user == null) return false;

        if (gallery.CreatedByUserId?.Equals(user.Id, StringComparison.InvariantCultureIgnoreCase) ?? false)
            return true;
        
        if (await _userManager.IsInRoleAsync(user, RoleKind.Administrator))
            return true;
        if (await _userManager.IsInRoleAsync(user, RoleKind.GalleryViewOverride))
            return true;
        if (await _userManager.IsInRoleAsync(user, RoleKind.GalleryAdmin))
            return true;
        
        return false;
    }
    
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string id)
    {
        id = id.Trim().ToLower();
        var galleryRecord = await _db.Galleries
            .Include(e => e.CreatedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (galleryRecord == null)
        {
            HttpContext.Response.StatusCode = 404;
            return View("NotFound");
        }

        if (await CanAccessGallery(user, galleryRecord) == false)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotAuthorized");
        }

        var files = await _db.GalleryFileAssociations
            .Include(e => e.File)
            .Include(e => e.File.ImageInfo)
            .Include(e => e.File.Preview)
            .AsNoTracking()
            .Where(e => e.GalleryId == galleryRecord.Id)
            .OrderBy(e => e.SortOrder)
            .Select(e => e.File)
            .ToListAsync();
        var author = galleryRecord.CreatedByUser;

        var vm = new IndexViewModel()
        {
            Author = author,
            Files = files,
            Gallery = galleryRecord
        };

        return View("Index", vm);
    }
}