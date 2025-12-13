using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Helpers;
using Kasta.Web.Models.Api.Request;
using Kasta.Web.Models.Api.Response;
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
    private readonly FileWebService _fileWebService;
    private readonly SystemSettingsProxy _systemSettings;

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
        _fileWebService = services.GetRequiredService<FileWebService>();
        _systemSettings = services.GetRequiredService<SystemSettingsProxy>();
        
        _logger = logger;
    }

    private async Task<UserModel?> GetUserOrFromToken(string? token)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null && !string.IsNullOrEmpty(token))
        {
            var u = await _db.UserApiKeys
                .AsNoTracking()
                .Where(e => e.Token == token)
                .Include(e => e.User)
                .FirstOrDefaultAsync();
            if (u != null)
            {
                user = u.User;
            }
        }

        return user;
    }

    [HttpGet("~/f/{value}")]
    public Task<IActionResult> GetFileShort(string value, [FromQuery] bool preview = false, [FromQuery] bool download = false)
    {
        return _fileWebService.DownloadFile(this, value, preview, download);
    }
    
    [HttpGet("~/api/v1/File/{value}/Download")]
    public Task<IActionResult> GetFile(string value, [FromQuery] bool preview = false)
    {
        return _fileWebService.DownloadFile(this, value, preview, true);
    }
    
    [HttpPost("~/api/v1/File/Upload/Form")]
    public async Task<IActionResult> UploadBasic(IFormFile file, [FromForm] string? filename = null, [FromForm] string? token = null)
    {
        var user = await GetUserOrFromToken(token);
        if (user == null)
        {
            return new JsonResult(new JsonErrorResponseModel()
            {
                Message = "Not Authorized"
            });
        }

        var userLimit = await _db.UserLimits
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == user.Id);
        if (_systemSettings.EnableQuota)
        {
            var spaceUsed = userLimit?.SpaceUsed ?? 0;
            if ((spaceUsed + file.Length) > (userLimit?.MaxStorage ?? _systemSettings.DefaultStorageQuota ?? 0))
            {
                HttpContext.Response.StatusCode = 401;
                return Json(
                    new JsonErrorResponseModel
                    {
                        Message = "Not enough storage to upload file."
                    });
            }

            if (file.Length > (userLimit?.MaxFileSize ?? _systemSettings.DefaultUploadQuota ?? long.MaxValue))
            {
                HttpContext.Response.StatusCode = 400;
                return Json(
                    new JsonErrorResponseModel
                    {
                        Message = $"Provided file exceeds maximum file size"
                    });
            }
        }
        
        FileModel data;
        await using (var stream = file.OpenReadStream())
        {
            var fn = file.FileName;
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
        var user = await GetUserOrFromToken(token);
        if (user == null)
        {
            return new JsonResult(new JsonErrorResponseModel()
            {
                Message = "Not Authorized"
            });
        }
        var file = await _db.GetFileAsync(id);
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
        [FromForm] CreateUploadSessionRequest sessionParams)
    {
        throw new NotImplementedException();
    }
}