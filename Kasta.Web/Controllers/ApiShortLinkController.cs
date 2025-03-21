using System.Text.Json;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Helpers;
using Kasta.Web.Models;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DeleteShortenedLinkResult = Kasta.Web.Services.LinkShortenerWebService.DeleteShortenedLinkResult;

namespace Kasta.Web.Controllers;

[ApiController]
public class ApiShortLinkController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;
    private readonly SignInManager<UserModel> _signInManager;
    private readonly ShortUrlService _shortUrlService;
    private readonly LinkShortenerWebService _linkShortenerWebService;
    
    private readonly ILogger<ApiShortLinkController> _logger;

    public ApiShortLinkController(IServiceProvider services, ILogger<ApiShortLinkController> logger)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _signInManager = services.GetRequiredService<SignInManager<UserModel>>();
        _shortUrlService = services.GetRequiredService<ShortUrlService>();
        _linkShortenerWebService = services.GetRequiredService<LinkShortenerWebService>();

        _logger = logger;
    }

    [HttpGet("~/api/v1/Link/{value}")]
    [HttpGet("~/l/{value}")]
    public async Task<IActionResult> RedirectToLinkDestination(string value)
    {
        var systemSettings = _db.GetSystemSettings();
        if (systemSettings.EnableLinkShortener == false)
        {
            HttpContext.Response.StatusCode = 403;
            return new ViewResult()
            {
                ViewName = "NotAuthorized"
            };
        }

        var model = await _db.ShortLinks.Where(e => e.Id == value).FirstOrDefaultAsync();
        model ??= await _db.ShortLinks.Where(e => e.ShortLink == value).FirstOrDefaultAsync();

        if (model == null)
        {
            HttpContext.Response.StatusCode = 404;
            return new ViewResult()
            {
                ViewName = "NotFound"
            };
        }

        if (string.IsNullOrEmpty(model.Destination))
        {
            _logger.LogError($"Blank destination for model {model.Id} ({model.GetType()})");
            HttpContext.Response.StatusCode = 404;
            return new ViewResult()
            {
                ViewName = "NotFound"
            };
        }

        return new RedirectResult(model.Destination);
    }

    [HttpPost("~/api/v1/Link/Create")]
    public async Task<IActionResult> Create(
        [FromForm] CreateShortLinkRequest contract,
        [FromQuery] string? token = null)
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
            HttpContext.Response.StatusCode = 403;
            return Json(new JsonErrorResponseModel()
            {
                Message = "Not Authorized"
            }, new JsonSerializerOptions() { WriteIndented = true });
        }

        var systemSettings = _db.GetSystemSettings();
        if (systemSettings.EnableLinkShortener == false)
        {
            HttpContext.Response.StatusCode = 400;
            return Json(new JsonErrorResponseModel()
            {
                Message = "Link shortener disabled"
            }, new JsonSerializerOptions() { WriteIndented = true });
        }
        
        var model = new ShortLinkModel()
        {
            CreatedByUserId = user.Id,
            Destination = contract.Destination,
            ShortLink = _shortUrlService.Generate(),
            IsVanity = false
        };
        if (!string.IsNullOrEmpty(contract.Vanity))
        {
            bool exists = await _db.ShortLinks.Where(e => e.ShortLink == contract.Vanity || e.Id == contract.Vanity).AnyAsync();
            if (exists)
            {
                HttpContext.Response.StatusCode = 400;
                return Json(new JsonErrorResponseModel()
                {
                    Message = "Link already exists"
                }, new JsonSerializerOptions() { WriteIndented = true });
            }

            model.ShortLink = contract.Vanity.Trim();
            model.IsVanity = true;
        }

        using (var ctx = _db.CreateSession())
        {
            var trans = ctx.Database.BeginTransaction();
            try
            {
                await ctx.ShortLinks.AddAsync(model);
                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                _logger.LogError($"Failed to insert model\n{ex}");
                throw;
            }
        }

        return Json(new Dictionary<string, object>()
        {
            {"url", $"{FeatureFlags.Endpoint}/l/{model.ShortLink}"},
            {"destination", model.Destination},
            {"id", model.Id},
            {"deleteUrl", $"{FeatureFlags.Endpoint}/api/v1/Link/{model.Id}/Delete" + (string.IsNullOrEmpty(token) ? "" : $"?token={token}")},
        }, new JsonSerializerOptions() { WriteIndented = true });
    }
    
    [HttpGet("~/api/v1/Link/{value}/Delete")]
    [HttpPost("~/api/v1/Link/{value}/Delete")]
    public async Task<IActionResult> Delete(
        string value,
        [FromQuery] string? token = null)
    {
        var result = await _linkShortenerWebService.Delete(_logger, value, this, token);
        switch (result)
        {
            case DeleteShortenedLinkResult.Success:
                HttpContext.Response.StatusCode = 201;
                return new EmptyResult();
            case DeleteShortenedLinkResult.NotAuthorized:
                HttpContext.Response.StatusCode = 403;
                return Json(new JsonErrorResponseModel()
                {
                    Message = $"Not Authorized"
                });
            case DeleteShortenedLinkResult.NotFound:
                HttpContext.Response.StatusCode = 404;
                return Json(new JsonErrorResponseModel()
                {
                    Message = $"Not Found"
                });
            default:
                throw new InvalidOperationException($"Unhandled value {result} for {typeof(DeleteShortenedLinkResult)}");
        }
    }
}