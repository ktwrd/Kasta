using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.S3.Model;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Helpers;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace Kasta.Web.Services;

public class FileWebService
{
    private readonly ApplicationDbContext _db;
    private readonly S3Service _s3;
    private readonly SignInManager<UserModel> _signInManager;
    private readonly UserManager<UserModel> _userManager;
    private readonly SystemSettingsProxy _systemSettingsProxy;
    private readonly ILogger<FileWebService> _logger;
    
    public FileWebService(IServiceProvider services, ILogger<FileWebService> logger)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _s3 = services.GetRequiredService<S3Service>();
        _signInManager = services.GetRequiredService<SignInManager<UserModel>>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _logger = logger;
    }
    
    private IActionResult ReturnNotFound(Controller controller)
    {
        var requestHeaders = controller.Request.GetTypedHeaders();
        if (requestHeaders.Accept.Any(e => e.MatchesMediaType(new("text/html"))))
        {
            return new ViewResult()
            {
                ViewName = "NotFound"
            };
        }
        else if (requestHeaders.Accept.Any(e => e.MatchesMediaType(new("application/json"))))
        {
            return new JsonResult(
                new Dictionary<string, object>()
                {
                    {
                        "error", "NotFound"
                    }
                }, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
        }
        else
        {
            return new ContentResult()
            {
                Content = "NotFound",
                StatusCode = 404,
                ContentType = "text/plain"
            };
        }
    }
    public async Task<IActionResult> DownloadFile(Controller context, string id, bool preview, bool downloadOnly)
    {
        var model = await _db.GetFileAsync(id);
        if (model == null)
        {
            context.HttpContext.Response.StatusCode = 404;
            return new ViewResult()
            {
                ViewName = "NotFound"
            };
        }

        if (!model.Public)
        {
            if (!_signInManager.IsSignedIn(context.User))
            {
                context.HttpContext.Response.StatusCode = 404;
                return new ViewResult()
                {
                    ViewName = "NotFound"
                };
            }

            var userModel = await _userManager.GetUserAsync(context.User);
            if ((userModel?.Id ?? "invalid") != model.CreatedByUserId)
            {
                if (!(userModel?.IsAdmin ?? false))
                {
                    context.Response.StatusCode = 404;
                    return new ViewResult()
                    {
                        ViewName = "NotFound"
                    };
                }
            }
        }
        
        
        string relativeLocation = model.RelativeLocation;
        string filename = model.Filename;
        string? mimeType = model.MimeType;
        var filePreview = await _db.FilePreviews
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == model.Id);
        if (filePreview != null && preview)
        {
            relativeLocation = filePreview.RelativeLocation;
            filename = filePreview.Filename;
            mimeType = filePreview.MimeType;
        }

        if (_systemSettingsProxy.S3UsePresignedUrl)
        {
            var url = await _s3.GeneratePresignedUrl(relativeLocation, TimeSpan.FromMinutes(15));
            context.Response.Headers.Location = new(url);
            context.Response.StatusCode = 302;
            return new EmptyResult();
        }
        var obj = await _s3.GetObject(relativeLocation);
        if (obj == null)
        {
            context.Response.StatusCode = 404;
            return new ViewResult()
            {
                ViewName = "NotFound"
            };
        } 
        
        context.Response.StatusCode = 200;
        context.Response.ContentLength = obj.ContentLength;
        context.Response.ContentType = mimeType ?? "application/octet-stream";
        context.Response.Headers.LastModified = obj.LastModified?.ToString("R");
        var disposition = new List<string>()
        {
            "attachment",
            $"filename=\"{filename}\"",
            $"filename=*UTF-8''" + WebUtility.UrlEncode(filename)
        };
        if (!downloadOnly)
        {
            disposition.Insert(0, "inline");
        }
        context.Response.Headers.ContentDisposition = new StringValues(
            string.Join(";", disposition));
        context.Response.Headers["Kasta-FileId"] = model.Id;
        if (model.CreatedByUserId != null)
        {
            context.Response.Headers["Kasta-AuthorId"] = model.CreatedByUserId;
        }
        if (context.Request.Method != "OPTIONS" || context.Request.Method != "HEAD")
        {
            await StreamCopyOperation.CopyToAsync(obj.ResponseStream, context.Response.Body, obj.ContentLength, context.HttpContext.RequestAborted);
        }
        return new EmptyResult();
    }
}