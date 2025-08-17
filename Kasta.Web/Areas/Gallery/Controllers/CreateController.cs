using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Data.Models.Gallery;
using Kasta.Data.Repositories;
using Kasta.Web.Areas.Gallery.Models.Create;
using Kasta.Web.Helpers;
using Kasta.Web.Models;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Areas.Gallery.Controllers;

[Area("Gallery")]
[Route("~/Gallery/Create")]
[AuthRequired]
public class CreateController : Controller, IGalleryController
{
    private readonly ApplicationDbContext _db;
    private readonly GalleryRepository _repo;
    private readonly UploadService _uploadService;
    private readonly UserManager<UserModel> _userManager;
    private readonly ILogger<GalleryDetailsController> _logger;

    #region IGalleryController
    public ApplicationDbContext Database => _db;
    public UserManager<UserModel> UserManager => _userManager;
    #endregion IGalleryController
    
    public CreateController(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _repo =  services.GetRequiredService<GalleryRepository>();
        _uploadService = services.GetRequiredService<UploadService>();
        _logger = services.GetRequiredService<ILogger<GalleryDetailsController>>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
    }
    
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotAuthorized");
        }
        
        var galleryRecord = await _repo.CreateDraftAsync(user);
        
        var vm = new CreateGalleryViewModel
        {
            Gallery = galleryRecord
        };
        
        return View("Create", vm);
    }
    
    [HttpGet("Component/Update")]
    public async Task<IActionResult> ComponentUpdateTitleGet(
        string id)
    {
        id = id.Trim().ToLower();
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return PartialView("NotAuthorized");
        }
        var model = await _db.Galleries
            .AsNoTracking()
            .Where(e => e.Id == id && e.CreatedByUserId == user.Id)
            .FirstOrDefaultAsync();
        if (model == null)
        {
            return PartialView("NotFound");
        }
        var vm = new CreateGalleryViewModel
        {
            Gallery = model,
        };
        return PartialView("ComponentUpdate", vm);
    }
    
    [HttpPost("Component/Update")]
    public async Task<IActionResult> ComponentUpdate(
        [FromForm] string id,
        [FromForm] string? title,
        [FromForm] string? description)
    {
        id = id.Trim().ToLower();
        
        title ??= "";
        description ??= "";
        
        title = title.Trim();
        description = description.Trim();
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return PartialView("NotAuthorized");
        }
        
        var model = await _db.Galleries
            .Include(e => e.CreatedByUser)
            .AsNoTracking()
            .Where(e => e.Id == id && e.CreatedByUserId == user.Id)
            .FirstOrDefaultAsync();
        if (model == null)
        {
            return PartialView("NotFound");
        }
        
        var vm = new CreateGalleryViewModel
        {
            Gallery = model,
        };
        
        vm.Gallery.Title = title;
        vm.Gallery.Description = description;
        
        var errorMessages = new List<string>();
        if (title.Length > 200)
        {
            errorMessages.Add($"Title is greater than 200 characters ({title.Trim().Length})");
        }
        if (description.Length > 4000)
        {
            errorMessages.Add($"Description is greater than 4000 characters ({description.Trim().Length})");
        }
        
        if (errorMessages.Count != 0)
        {
            vm.Alert = new()
            {
                AlertContent = string.Join("\n",
                    "**Validation Error**",
                    string.Join("\n", errorMessages.Select(e => "- " + e))),
                AlertContentAsMarkdown = true,
                AlertIsSmall = true,
                AlertType = "warning",
                ShowAlertCloseButton = true
            };
            return PartialView("ComponentUpdate");
        }
        
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var desc = string.IsNullOrEmpty(description) ? null : description.Trim();
            await ctx.Galleries
                .Where(e => e.Id == model.Id)
                .ExecuteUpdateAsync(e
                    => e.SetProperty(p => p.Title, title)
                        .SetProperty(p => p.Description, desc));
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            _logger.LogError(ex, "Failed to update Title as \"{Title}\" for GalleryModel with Id {Id}", title, id);
            vm.Alert = new()
            {
                AlertType = "danger",
                AlertContent = $"Failed to update:\n```\n{ex.Message}\n```",
                AlertContentAsMarkdown = true,
                ShowAlertCloseButton = true,
                AlertIsSmall = true
            };
        }
        return PartialView("ComponentUpdate", vm);
    }
    
    [HttpPost("UploadFile")]
    public async Task<IActionResult> UploadFile(
        [FromForm] string id,
        IFormFile file,
        [FromQuery] bool returnJson = false)
    {
        id = id.Trim().ToLower();
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            HttpContext.Response.StatusCode = 403;
            var vm = new NotAuthorizedViewModel()
            {
                Message = $"You do not have permission to access this page."
            };
            if (returnJson)
            {
                return Json(vm);
            }
            return View("NotAuthorized", vm);
        }
        var gallery = await _db.Galleries
            .AsNoTracking()
            .Where(e => e.Id == id && e.CreatedByUserId == user.Id)
            .FirstOrDefaultAsync();
        if (gallery == null)
        {
            HttpContext.Response.StatusCode = 404;
            var vm = new NotFoundViewModel()
            {
                Message = "Gallery not found."
            };
            if (returnJson)
            {
                return Json(vm);
            }
            return View("NotFound", vm);
        }
        
        var result = await FileHelper.HandleUpload(
            _db,
            _uploadService,
            user,
            file);
        if (result.IsFailure)
        {
            HttpContext.Response.StatusCode = 401;
            if (returnJson)
            {
                return Json(result.Error);
            }
            return View("NotAuthorized", result.Error);
        }
        var resultFile = result.Value;
        
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var associationCount = await ctx.GalleryFileAssociations.Where(e => e.GalleryId == gallery.Id).CountAsync();
            var record = new GalleryFileAssociationModel()
            {
                GalleryId = gallery.Id,
                FileId = resultFile.Id,
                SortOrder = associationCount + 1
            };
            await ctx.GalleryFileAssociations.AddAsync(record);
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            _logger.LogError(ex, "Failed to associate file {FileId} with Gallery {GalleryId}", resultFile.Id, gallery.Id);
        }
        
        var fileUrl = Url.Action("GetFile", "GalleryDetails", new
        {
            Area = "Gallery",
            galleryId = gallery.Id,
            fileId = resultFile.Id,
        });
        if (fileUrl == null)
            throw new InvalidOperationException("Failed to generate URL for GalleryDetails.GetFile");
        
        if (returnJson)
        {
            return Json(new Dictionary<string, object>()
            {
                { "message", "OK" },
                { "url", fileUrl }
            });
        }
        return RedirectToAction("GetFile", "GalleryDetails", new
        {
            Area = "Gallery",
            galleryId = gallery.Id,
            fileId = resultFile.Id,
        });
    }
}