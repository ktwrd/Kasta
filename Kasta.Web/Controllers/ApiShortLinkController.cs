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

namespace Kasta.Web.Controllers;

[ApiController]
public class ApiShortLinkController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;
    private readonly SignInManager<UserModel> _signInManager;
    private readonly ShortUrlService _shortUrlService;
    
    private readonly ILogger<ApiShortLinkController> _logger;

    public ApiShortLinkController(IServiceProvider services, ILogger<ApiShortLinkController> logger)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _signInManager = services.GetRequiredService<SignInManager<UserModel>>();
        _shortUrlService = services.GetRequiredService<ShortUrlService>();

        _logger = logger;
    }

    [HttpGet("~/api/v1/Link/{value}")]
    [HttpGet("~/l/{value}")]
    public async Task<IActionResult> RedirectToLinkDestination(string value)
    {
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
        if (!string.IsNullOrEmpty(contract.ShortLinkName))
        {
            bool exists = await _db.ShortLinks.Where(e => e.ShortLink == contract.ShortLinkName || e.Id == contract.ShortLinkName).AnyAsync();
            if (exists)
            {
                HttpContext.Response.StatusCode = 400;
                return Json(new JsonErrorResponseModel()
                {
                    Message = "Link already exists"
                }, new JsonSerializerOptions() { WriteIndented = true });
            }

            model.ShortLink = contract.ShortLinkName.Trim();
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
            });
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

        if (model.CreatedByUserId != user.Id)
        {
            var adminRoleId = await _db.Roles.Where(e => e.NormalizedName == RoleKind.Administrator.ToUpper()).Select(e => e.Id).FirstOrDefaultAsync();
            if (adminRoleId != null)
            {
                if (await _db.UserRoles.Where(e => e.UserId == user.Id && e.RoleId == adminRoleId).AnyAsync() == false)
                {
                    HttpContext.Response.StatusCode = 403;
                    return Json(new JsonErrorResponseModel()
                    {
                        Message = "You do not have permission to delete this URL"
                    });
                }
            }
        }

        using (var ctx = _db.CreateSession())
        {
            var trans = await ctx.Database.BeginTransactionAsync();
            try
            {
                await ctx.ShortLinks.Where(e => e.Id == model.Id).ExecuteDeleteAsync();
                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                _logger.LogError($"Failed to delete ShortLink {model.Id}\n{ex}");
                throw;
            }
        }

        HttpContext.Response.StatusCode = 201;
        return new EmptyResult();
    }
}