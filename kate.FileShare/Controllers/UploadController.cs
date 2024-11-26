using kate.FileShare.Data;
using kate.FileShare.Data.Models;
using kate.FileShare.Models;
using kate.FileShare.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace kate.FileShare.Controllers;

[ApiController]
[Route("/api/v1/Upload")]
public class UploadController : Controller
{
    private readonly S3Service _s3;
    private readonly UploadService _uploadService;
    private readonly ILogger<UploadController> _logger;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;

    public UploadController(
        S3Service s3,
        UploadService uploadService,
        ILogger<UploadController> logger,
        ApplicationDbContext db,
        UserManager<UserModel> userManager)
    {
        _s3 = s3;
        _uploadService = uploadService;
        _logger = logger;
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("~/f/{value}")]
    public async Task<IActionResult> GetFile(string value, [FromQuery] bool preview = false)
    {
        var model = await _db.Files.Where(v => v.Id == value).Include(fileModel => fileModel.Preview).FirstOrDefaultAsync();
        model ??= await _db.Files.Where(v => v.ShortUrl == value).Include(fileModel => fileModel.Preview).FirstOrDefaultAsync();
        if (model == null)
        {
            HttpContext.Response.StatusCode = 404;
            return View("NotFound");
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

    [AuthRequired]
    [HttpPost("Form")]
    public async Task<IActionResult> UploadBasic(IFormFile file)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            throw new InvalidOperationException($"Unable to fetch User model even though user is logged in?");
        }
        FileModel data;
        using (var stream = file.OpenReadStream())
        {
            data = await _uploadService.UploadBasicAsync(user, stream, file.FileName, file.Length);
        }

        return Json(new FileJsonResponseModel()
        {
            Id = data.Id,
            Url = $"{FeatureFlags.Endpoint}/f/{data.ShortUrl}",
            Filename = data.Filename,
            FileSize = data.Size,
            CreatedAtTimestamp = data.CreatedAt.ToUnixTimeSeconds()
        });
    }

    [AuthRequired]
    [HttpPost("Chunk/StartSession")]
    public async Task<IActionResult> StartSession(
        [FromForm] CreateSessionParams sessionParams)
    {
        throw new NotImplementedException();
    }
}