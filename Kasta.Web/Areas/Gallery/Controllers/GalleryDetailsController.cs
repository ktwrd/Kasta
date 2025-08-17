using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Data.Repositories;
using Kasta.Web.Areas.Gallery.Models.Details;
using Kasta.Web.Helpers;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Areas.Gallery.Controllers;

[Area("Gallery")]
[Route("~/Gallery/Details")]
public class GalleryDetailsController : Controller, IGalleryController
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;
    private readonly FileService _fileService;
    private readonly ILogger<GalleryDetailsController> _logger;
    private readonly GalleryRepository _repo;
    private readonly GalleryService _service;

    #region IGalleryController
    public ApplicationDbContext Database => _db;
    public UserManager<UserModel> UserManager => _userManager;
    #endregion IGalleryController
    
    public GalleryDetailsController(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _logger = services.GetRequiredService<ILogger<GalleryDetailsController>>();
        _fileService = services.GetRequiredService<FileService>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _repo = services.GetRequiredService<GalleryRepository>();
        _service = services.GetRequiredService<GalleryService>();
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> Index(
        string id)
    {
        var galleryRecord = await _repo.GetById(id);
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (galleryRecord == null)
        {
            HttpContext.Response.StatusCode = 404;
            return View("NotFound");
        }

        if (await this.CanAccessGalleryAsync(user, galleryRecord) == false)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotAuthorized");
        }

        var files = await _repo.GetFilesForGallery(galleryRecord);
        var author = galleryRecord.CreatedByUser;

        var userSetting = user == null ? new()
        {
            Id = user?.Id ?? Guid.Empty.ToString(),
        } : await Database.GetUserSettingsAsync(user);
        
        var vm = new IndexViewModel()
        {
            Author = author,
            Files = files,
            Gallery = galleryRecord,
            UserSettings = userSetting
        };
        
        vm.CanEdit = author != null && author.Id.Equals(user?.Id ?? "", StringComparison.OrdinalIgnoreCase);
        if (!vm.CanEdit && user != null)
        {
            
        }

        return View("Index", vm);
    }
    
    [HttpGet("{galleryId}/File/{fileId}")]
    public async Task<IActionResult> GetFile(
        string galleryId,
        string fileId)
    {
        var galleryRecord = await _repo.GetById(galleryId);
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (galleryRecord == null)
        {
            HttpContext.Response.StatusCode = 404;
            return View("NotFound");
        }
        
        if (await this.CanAccessGalleryAsync(user, galleryRecord) == false)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotAuthorized");
        }
        
        var file = await _db.GetFileAsync(fileId, includeAuthor: true, includePreview: true, includeImageInfo: true);
        if (file == null)
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }
        
        return await FileHelper.HandleDetailsResult(this, file, _db, _fileService);
    }
    
    [HttpGet("{galleryId}/Delete")]
    public async Task<IActionResult> Delete(
        string galleryId,
        [FromQuery] bool? confirm)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotAuthorized");
        }
        
        var galleryRecord = await _repo.GetById(galleryId);
        if (galleryRecord == null)
        {
            HttpContext.Response.StatusCode = 404;
            return View("NotFound");
        }
        
        if (await this.CanEditGalleryAsync(user, galleryRecord) == false)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotAuthorized");
        }
        
        if (confirm.GetValueOrDefault(false))
        {
            await _service.DeleteAsync(galleryRecord, user);
            return RedirectToAction("Index", "GalleryHome", new { Area = "Gallery" });
        }
        else
        {
            var fileCount = await _db.GalleryFileAssociations
                .Where(e => e.GalleryId == galleryRecord.Id).CountAsync();
            var vm = new DeleteConfirmViewModel()
            {
                AffectedFiles = fileCount,
                Record = galleryRecord,
            };
            return View("DeleteConfirm", vm);
        }
    }
}