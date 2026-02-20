using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Models;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Controllers;

[Authorize]
[Route("~/[controller]")]
public class LinkShortenerController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;
    private readonly LinkShortenerWebService _linkShortenerWebService;
    private readonly SystemSettingsProxy _systemSettings;
    private readonly ILogger<LinkShortenerController> _logger;

    public LinkShortenerController(IServiceProvider services, ILogger<LinkShortenerController> logger)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _linkShortenerWebService = services.GetRequiredService<LinkShortenerWebService>();
        _systemSettings = services.GetRequiredService<SystemSettingsProxy>();

        _logger = logger;
    }
    
    
    [HttpGet("List")]
    [Authorize]
    public async Task<IActionResult> Index([FromQuery] int? page = 1)
    {
        if (!ModelState.IsValid) throw new InvalidOperationException("Model state is not valid");
        if (!_systemSettings.EnableLinkShortener)
        {
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "Link Shortener is disabled"
            });
        }
        var vm = new LinkListViewModel();
        if (page.HasValue && page.Value >= 1)
        {
            vm.Page = page.Value;
        }
        var query = _db.ShortLinks
            .AsNoTracking()
            .OrderByDescending(v => v.CreatedAt);
        (vm.Links, vm.IsLastPage) = await _db.PaginateAsync(query, vm.Page, 50);
        return View("Index", vm);
    }

    [HttpGet("Delete")]
    [Authorize]
    public async Task<IActionResult> Delete([FromQuery] string value, [FromQuery] string? returnUrl = null)
    {
        if (!ModelState.IsValid) throw new InvalidOperationException("Model state is not valid");
        if (!_systemSettings.EnableLinkShortener)
        {
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "Link Shortener is disabled"
            });
        }
        
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotAuthorized");
        }

        var model = await _db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == value);
        model ??= await _db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ShortLink == value);

        if (model == null)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotFound");
        }

        if (model.CreatedByUserId != user.Id)
        {
            if (await _userManager.IsInRoleAsync(user, RoleKind.Administrator) == false)
            {
                HttpContext.Response.StatusCode = 403;
                return View("NotAuthorized");
            }
        }

        await using (var ctx = _db.CreateSession())
        {
            var trans = await ctx.Database.BeginTransactionAsync();
            try
            {
                await _db.ShortLinks.Where(e => e.Id == model.Id).ExecuteDeleteAsync();
                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }
        
        if (!string.IsNullOrEmpty(returnUrl))
        {
            return new RedirectResult(returnUrl);
        }
        return new RedirectToActionResult(nameof(Index), "Home", null);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> CreateLink(string link, string? vanity = null)
    {
        if (!ModelState.IsValid) throw new InvalidOperationException("Model state is not valid");
        if (string.IsNullOrEmpty(vanity?.Trim())) vanity = null;
        if (!_systemSettings.EnableLinkShortener)
        {
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "Link Shortener is disabled"
            });
        }
        
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            HttpContext.Response.StatusCode = 403;
            return View("NotAuthorized");
        }

        throw new NotImplementedException();
    }
}