using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Helpers;
using Kasta.Web.Models;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Controllers;

[ApiController]
public class ApiFileController : Controller
{
    private readonly S3Service _s3;
    private readonly UploadService _uploadService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;
    private readonly SignInManager<UserModel> _signInManager;
    private readonly FileService _fileService;

    private readonly ILogger<ApiFileController> _logger;
    
    public ApiFileController(
        IServiceProvider services,
        ILogger<ApiFileController> logger)
    {
        _s3 = services.GetRequiredService<S3Service>();
        _uploadService = services.GetRequiredService<UploadService>();
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _signInManager = services.GetRequiredService<SignInManager<UserModel>>();
        _fileService = services.GetRequiredService<FileService>();
        
        _logger = logger;
    }

    [HttpGet("~/f/{value}")]
    public IActionResult GetFileShort(string value, [FromQuery] bool preview = false)
    {
        var s = "";
        if (preview)
            s = "?preview=true";
        return Redirect($"/api/v1/File/{value}/Download{s}");
    }
    
    [HttpGet("~/api/v1/File/{value}/Download")]
    public async Task<IActionResult> GetFile(string value, [FromQuery] bool preview = false)
    {
        var model = await _db.Files.Where(v => v.Id == value).Include(fileModel => fileModel.Preview).FirstOrDefaultAsync();
        model ??= await _db.Files.Where(v => v.ShortUrl == value).Include(fileModel => fileModel.Preview).FirstOrDefaultAsync();
        if (model == null)
        {
            HttpContext.Response.StatusCode = 404;
            return View("NotFound");
        }
        if (!model.Public)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                Response.StatusCode = 404;
                return View("NotFound");
            }
            var userModel = await _userManager.GetUserAsync(User);

            if ((userModel?.Id ?? "invalid") != model.CreatedByUserId)
            {
                if (!(userModel?.IsAdmin ?? false))
                {
                    Response.StatusCode = 404;
                    return View("NotFound");
                }
            }
        }

        string relativeLocation = model.RelativeLocation;
        string filename = model.Filename;
        string? mimeType = model.MimeType;
        if (model.Preview != null && preview)
        {
            relativeLocation = model.Preview.RelativeLocation;
            filename = model.Preview.Filename;
            mimeType = model.Preview.MimeType;
        }
        var obj = await _s3.GetObject(relativeLocation);
        if (obj == null)
        {
            HttpContext.Response.StatusCode = 404;
            return View("NotFound");
        }
        
        HttpContext.Response.StatusCode = 200;
        return new FileStreamResult(obj.ResponseStream, mimeType ?? "application/octet-stream")
        {
            FileDownloadName = filename,
            LastModified = new DateTimeOffset(obj.LastModified)
        };
    }
    
    [HttpPost("~/api/v1/File/Upload/Form")]
    public async Task<IActionResult> UploadBasic(IFormFile file, [FromForm] string? filename = null, [FromForm] string? token = null)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null && !string.IsNullOrEmpty(token))
        {
            var u = await _db.UserApiKeys.Where(e => e.Token == token).Include(e => e.User).FirstOrDefaultAsync();
            if (u != null)
            {
                user = u.User;
            }
        }
        if (user == null)
        {
            return new JsonResult(new JsonErrorResponseModel()
            {
                Message = "Not Authorized"
            });
        }

        var userLimit = await _db.UserLimits.Where(e => e.UserId == user.Id).FirstOrDefaultAsync();
        var systemSettings = _db.GetSystemSettings();
        if (systemSettings.EnableQuota)
        {
            long spaceUsed = userLimit?.SpaceUsed ?? 0;
            if ((spaceUsed + file.Length) > (userLimit?.MaxStorage ?? systemSettings.DefaultStorageQuotaReal ?? 0))
            {
                HttpContext.Response.StatusCode = 401;
                return Json(
                    new JsonErrorResponseModel()
                    {
                        Message = "Not enough storage to upload file."
                    });
            }

            if (file.Length > (userLimit?.MaxFileSize ?? systemSettings.DefaultUploadQuotaReal ?? long.MaxValue))
            {
                HttpContext.Response.StatusCode = 400;
                return Json(
                    new JsonErrorResponseModel()
                    {
                        Message = $"Provided file exceeds maximum file size"
                    });
            }
        }
        
        FileModel data;
        using (var stream = file.OpenReadStream())
        {
            string fn = file.FileName;
            if (!string.IsNullOrEmpty(filename))
            {
                fn = filename;
            }
            data = await _uploadService.UploadBasicAsync(user, stream, fn, file.Length);
        }

        return Json(new FileJsonResponseModel()
        {
            Id = data.Id,
            Url = $"{FeatureFlags.Endpoint}/f/{data.ShortUrl}",
            DetailsUrl = $"{FeatureFlags.Endpoint}/d/{data.ShortUrl}",
            DeleteUrl = $"{FeatureFlags.Endpoint}/api/v1/File/{data.Id}/Delete",
            Filename = data.Filename,
            FileSize = data.Size,
            CreatedAtTimestamp = data.CreatedAt.ToUnixTimeSeconds()
        });
    }
    
    [HttpGet("~/api/v1/File/{id}/Delete")]
    [HttpDelete("~/api/v1/File/{id}/Delete")]
    [HttpPost("~/api/v1/File/{id}/Delete")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string? token = null)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null && !string.IsNullOrEmpty(token))
        {
            var u = await _db.UserApiKeys.Where(e => e.Token == token).Include(e => e.User).FirstOrDefaultAsync();
            if (u != null)
            {
                user = u.User;
            }
        }
        if (user == null)
        {
            return new JsonResult(new JsonErrorResponseModel()
            {
                Message = "Not Authorized"
            });
        }
        var file = await _db.Files.Where(v => v.Id == id).Include(e => e.CreatedByUser).FirstOrDefaultAsync();
        file ??= await _db.Files.Where(v => v.ShortUrl == id).Include(e => e.CreatedByUser).FirstOrDefaultAsync();
        if (file == null)
        {
            Response.StatusCode = 404;
            return Json(new JsonErrorResponseModel()
            {
                Message = "File Not Found"
            });
        }
        if (file.CreatedByUserId != user.Id && !user.IsAdmin)
        {
            Response.StatusCode = 403;
            return Json(new JsonErrorResponseModel()
            {
                Message = "Not Authorized"
            });
        }
        await _fileService.DeleteFile(user, file);
        return new EmptyResult();
    }
    
    [AuthRequired]
    [HttpPost("~/api/v1/File/Upload/Chunk/StartSession")]
    public IActionResult StartSession(
        [FromForm] CreateSessionParams sessionParams)
    {
        throw new NotImplementedException();
    }
}