using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Models.Api.Request;
using Kasta.Web.Models.Api.Response;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeoSmart.PrettySize;

namespace Kasta.Web.Controllers;

[ApiController]
public class ApiFileController : Controller
{
    private readonly UploadService _uploadService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;
    private readonly FileService _fileService;
    private readonly FileWebService _fileWebService;
    private readonly SystemSettingsProxy _systemSettings;
    private readonly UserService _userService;

    private readonly ILogger<ApiFileController> _logger;
    
    public ApiFileController(
        IServiceProvider services,
        ILogger<ApiFileController> logger)
    {
        _uploadService = services.GetRequiredService<UploadService>();
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _fileService = services.GetRequiredService<FileService>();
        _fileWebService = services.GetRequiredService<FileWebService>();
        _systemSettings = services.GetRequiredService<SystemSettingsProxy>();
        _userService = services.GetRequiredService<UserService>();
        
        _logger = logger;
    }

    [HttpGet("~/f/{value}")]
    [HttpGet("~/f/{value}/{filename}")]
    public Task<IActionResult> GetFileShort(string value, string? filename = null, [FromQuery] bool preview = false, [FromQuery] bool download = false)
    {
        return _fileWebService.DownloadFile(this, value, preview, download, renameToFile: filename);
    }
    [HttpGet("~/api/v1/File/{value}/Download")]
    public Task<IActionResult> GetFile(string value, [FromQuery] bool preview = false)
    {
        return _fileWebService.DownloadFile(this, value, preview, true);
    }
    
    [AuthRequired]
    [HttpPost("~/api/v1/File/Upload/Form")]
    public async Task<IActionResult> UploadBasic(IFormFile file, [FromForm] string? filename = null)
    {
        var user = await _userService.GetCurrentUser();
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
            var freeSpace = (userLimit?.MaxStorage ?? _systemSettings.DefaultStorageQuota ?? 0) - spaceUsed;
            if (freeSpace - file.Length <= 0)
            {
                HttpContext.Response.StatusCode = 401;
                return Json(
                    new JsonErrorResponseModel
                    {
                        Message = $"Not enough storage to upload file. You're short by: {PrettySize.Bytes(file.Length - freeSpace)}"
                    });
            }

            var maxUploadSize = userLimit?.MaxFileSize ?? _systemSettings.DefaultUploadQuota ?? long.MaxValue;

            if (file.Length > maxUploadSize)
            {
                var maxUploadSizeFormatted = maxUploadSize.ToString("n0");
                HttpContext.Response.StatusCode = 400;
                return Json(
                    new JsonErrorResponseModel
                    {
                        Message = $"Provided file exceeds maximum file size: {PrettySize.Bytes(maxUploadSize)} ({maxUploadSizeFormatted})"
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

        _logger.LogInformation("User {AuthorEmail} ({AuthorId}) uploaded {SizeFormatted} file {FileId}",
            user.Email,
            user.Id,
            PrettySize.Bytes(data.Size),
            data.Id);

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
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userService.GetCurrentUser();
        if (user == null)
        {
            Response.StatusCode = 403;
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
        // when requestor isn't author, or admin, or FileAdmin
        if (file.CreatedByUserId != user.Id
            && (!user.IsAdmin || !await _userManager.IsInRoleAsync(user, RoleKind.FileAdmin)))
        {
            Response.StatusCode = 403;
            return Json(new JsonErrorResponseModel()
            {
                Message = "Not Authorized"
            });
        }
        await _fileService.DeleteFile(user, file);
        _logger.LogInformation("User {DeletedByUserEmail} {DeletedByUserId} deleted file \"{FileName}\" {FileId} (author: {FileAuthorEmail} {FileAuthorId}, created at: {FileCreatedAt}, size: {SizeFormatted}/{SizeRaw})",
            user.Email,
            user.Id,
            file.Filename,
            file.Id,
            file.CreatedByUser?.Email,
            file.CreatedByUser?.Id,
            file.CreatedAt,
            PrettySize.Bytes(file.Size),
            file.Size);
        Response.StatusCode = 204;
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