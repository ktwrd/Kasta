using System.Text.Json;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Helpers;
using Kasta.Web.Models;
using Kasta.Web.Models.Api.Request;
using Kasta.Web.Models.Api.Response;
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
    private readonly ShortUrlService _shortUrlService;
    private readonly LinkShortenerWebService _linkShortenerWebService;
    private readonly SystemSettingsProxy _systemSettingsProxy;
    private readonly UserService _userService;
    
    private readonly ILogger<ApiShortLinkController> _logger;

    public ApiShortLinkController(IServiceProvider services, ILogger<ApiShortLinkController> logger)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _shortUrlService = services.GetRequiredService<ShortUrlService>();
        _linkShortenerWebService = services.GetRequiredService<LinkShortenerWebService>();
        _systemSettingsProxy = services.GetRequiredService<SystemSettingsProxy>();
        _userService = services.GetRequiredService<UserService>();

        _logger = logger;
    }

    [HttpGet("~/api/v1/Link/{value}")]
    [HttpGet("~/l/{value}")]
    [Produces("text/html", "application/json", "text/plain")]
    [ProducesResponseType(StatusCodes.Status307TemporaryRedirect)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RedirectToLinkDestination(string value)
    {
        if (!_systemSettingsProxy.EnableLinkShortener)
        {
            HttpContext.Response.StatusCode = 403;
            var message = "\"Link Shortener\" feature is unavailable.";
            if (HttpContext.Request.Headers.Accept.Contains("application/json"))
            {
                return Json(new JsonErrorResponseModel()
                {
                    Message = message
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            else if (HttpContext.Request.Headers.Accept.Contains("text/plain"))
            {
                return Content($"{HttpContext.Response.StatusCode}: {message}", "text/plain");
            }
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = message
            });
        }

        var model = await _db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == value);
        model ??= await _db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ShortLink == value);

        if (model == null)
        {
            HttpContext.Response.StatusCode = 404;
            return new ViewResult
            {
                ViewName = "NotFound"
            };
        }

        if (!string.IsNullOrEmpty(model.Destination)) return new RedirectResult(model.Destination, false);
        
        _logger.LogError("Blank destination for model {ModelId} (type: {ModelType})",
            model.Id,
            model.GetType());
        HttpContext.Response.StatusCode = 404;
        return new ViewResult()
        {
            ViewName = "NotFound"
        };
    }

    [AuthRequired]
    [HttpPost("~/api/v1/Link/Create")]
    [Produces("application/json")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromForm] CreateShortLinkRequest contract,
        [FromQuery] string? token = null)
    {
        var user = await _userService.GetCurrentUser();
        if (user == null)
        {
            HttpContext.Response.StatusCode = 403;
            return Json(new JsonErrorResponseModel()
            {
                Message = "Not Authorized"
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        if (!_systemSettingsProxy.EnableLinkShortener)
        {
            HttpContext.Response.StatusCode = 400;
            return Json(new JsonErrorResponseModel()
            {
                Message = "Link shortener disabled"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        
        var model = new ShortLinkModel()
        {
            CreatedByUserId = user.Id,
            Destination = contract.Destination,
            ShortLink = _shortUrlService.Generate(),
            IsVanity = false
        };
        if (!string.IsNullOrEmpty(contract.Vanity?.Trim()))
        {
            var exists = await _db.ShortLinks
                .AnyAsync(e => e.ShortLink == contract.Vanity || e.Id == contract.Vanity);
            if (exists)
            {
                HttpContext.Response.StatusCode = 400;
                return Json(new JsonErrorResponseModel()
                {
                    Message = $"Vanity Link already exists: {contract.Vanity}"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            model.ShortLink = contract.Vanity.Trim();
            model.IsVanity = true;
        }

        await using (var ctx = _db.CreateSession())
        {
            await using var trans = await ctx.Database.BeginTransactionAsync();
            try
            {
                await ctx.ShortLinks.AddAsync(model);
                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create shortlink to \"{ShortLinkDestination}\" for user {User} ({UserId})",
                    contract.Destination,
                    user,
                    user.Id);
                await trans.RollbackAsync();

                HttpContext.Response.StatusCode = 500;
                return Json(new JsonErrorResponseModel()
                {
                    Message = "Failed to create link",
                    Exception = ex.ToString()
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        var currentApiKey = await _userService.GetCurrentApiKey();

        return Json(new Dictionary<string, object>()
        {
            { "url", $"{FeatureFlags.Endpoint}/l/{model.ShortLink}" },
            { "destination", model.Destination },
            { "id", model.Id },
            { "deleteUrl", $"{FeatureFlags.Endpoint}/api/v1/Link/{model.Id}/Delete" + (string.IsNullOrEmpty(currentApiKey?.Token) ? "" : $"?token={currentApiKey?.Token}") },
        }, new JsonSerializerOptions() { WriteIndented = true });
    }
    
    [AuthRequired]
    [HttpGet("~/api/v1/Link/{value}/Delete")]
    [HttpPost("~/api/v1/Link/{value}/Delete")]
    [HttpDelete("~/api/v1/Link/{value}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        string value,
        [FromQuery] string? token = null)
    {
        var result = await _linkShortenerWebService.Delete(_logger, value, this, token);
        switch (result)
        {
            case DeleteShortenedLinkResult.Success:
                HttpContext.Response.StatusCode = 204;
                return new EmptyResult();
            case DeleteShortenedLinkResult.NotAuthorized:
                HttpContext.Response.StatusCode = 403;
                return Json(new JsonErrorResponseModel
                {
                    Message = "Not Authorized"
                });
            case DeleteShortenedLinkResult.NotFound:
                HttpContext.Response.StatusCode = 404;
                return Json(new JsonErrorResponseModel
                {
                    Message = "Not Found"
                });
            default:
                throw new InvalidOperationException($"Unhandled value {result} for {typeof(DeleteShortenedLinkResult)}");
        }
    }
}