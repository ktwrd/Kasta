using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Helpers;
using Kasta.Web.Models;
using Kasta.Web.Models.Components;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CreateShortenedLinkResult = Kasta.Web.Services.LinkShortenerWebService.CreateShortenedLinkResult;
using DeleteShortenedLinkResult = Kasta.Web.Services.LinkShortenerWebService.DeleteShortenedLinkResult;

namespace Kasta.Web.Controllers;

[Authorize]
[Route("~/[controller]")]
public class LinkShortenerController : Controller
{
    private readonly KastaDbContext _db;
    private readonly SystemSettingsProxy _systemSettings;
    private readonly LinkShortenerWebService _linkShortenerWebService;

    private readonly ILogger<LinkShortenerController> _logger;

    public LinkShortenerController(IServiceProvider services, ILogger<LinkShortenerController> logger)
    {
        _db = services.GetRequiredService<KastaDbContext>();
        _systemSettings = services.GetRequiredService<SystemSettingsProxy>();
        _linkShortenerWebService = services.GetRequiredService<LinkShortenerWebService>();

        _logger = logger;
    }

    private async Task<LinkListViewModel> GetViewModel(int? page = 1)
    {
        var vm = new LinkListViewModel
        {
            Page = Math.Max(1, page.GetValueOrDefault(0))
        };

        var query = _db.ShortLinks
            .AsNoTracking()
            .OrderByDescending(v => v.CreatedAt);
        (vm.Links, vm.IsLastPage) = await _db.PaginateAsync(query, vm.Page, 50);
        return vm;
    }

    [Authorize]
    [HttpGet]
    [HttpGet("List")]
    public async Task<IActionResult> Index([FromQuery] int? page = 1)
    {
        if (!_systemSettings.EnableLinkShortener)
        {
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "Link Shortener is disabled"
            });
        }
        var vm = await GetViewModel(page);
        return View("Index", vm);
    }

    [Authorize]
    [HttpPost("Delete")]
    public async Task<IActionResult> Delete(
        [FromForm] string value,
        [FromForm] int? page = null)
    {
        var result = await _linkShortenerWebService.Delete(_logger, value, this);
        var vm = await GetViewModel(page);
        switch (result)
        {
            case DeleteShortenedLinkResult.Success:
                vm.Alert = new()
                {
                    AlertContent = "Successfully deleted link!",
                    ShowAlertCloseButton = true,
                    AlertType = "success"
                };
                break;
            case DeleteShortenedLinkResult.NotFound:
                vm.Alert = new()
                {
                    AlertContent = string.Format(
                        "Could not find link with Id or Vanity of: `{0}`",
                        KastaWebHelper.HtmlSanitizeStrict(value)),
                    ShowAlertCloseButton = true,
                    AlertContentAsMarkdown = true,
                    AlertType = "warning"
                };
                break;
            case DeleteShortenedLinkResult.NotAuthorizedFeatureDisabled:
                if (!HttpContext.IsHtmxRequest())
                {
                    HttpContext.Response.StatusCode = 403;
                }
                return PartialView("NotAuthorized", new NotAuthorizedViewModel()
                {
                    Message = "Link Shortener is disabled."
                });
            case DeleteShortenedLinkResult.NotAuthorizedNotLoggedIn:
                if (!HttpContext.IsHtmxRequest())
                {
                    HttpContext.Response.StatusCode = 403;
                }
                return PartialView("NotAuthorized", new NotAuthorizedViewModel
                {
                    RequireLogin = true
                });
            case DeleteShortenedLinkResult.NotAuthorized:
                vm.Alert = new()
                {
                    AlertContent = "You do not have permission to delete this link.",
                    ShowAlertCloseButton = true,
                    AlertType = "danger"
                };
                break;
            default:
                throw new NotImplementedException($"Where {nameof(DeleteShortenedLinkResult)}={result}");
        }

        return PartialView("Index", vm);
    }

    [Authorize]
    [HttpPost("Create")]
    public async Task<IActionResult> CreateLink(
        [FromForm] string link,
        [FromForm] string? vanity = null,
        [FromForm] bool useVanity = false,
        [FromForm] int? page = null)
    {
        if (string.IsNullOrEmpty(vanity?.Trim()) || !useVanity) vanity = null;

        var serviceResult = await _linkShortenerWebService.Create(
            _logger,
            this,
            link, vanity);

        BaseAlertViewModel? alert = null;
        var alertIsForModal = true;
        switch (serviceResult)
        {
            case CreateShortenedLinkResult.Success:
                alert = new()
                {
                    AlertContent = serviceResult.ToDescriptionString("Successfully created link."),
                    AlertType = "success",
                    ShowAlertCloseButton = true
                };
                alertIsForModal = false;
                break;
            case CreateShortenedLinkResult.InvalidUri:
                alert = new()
                {
                    AlertContent = serviceResult.ToDescriptionString("Invalid URL"),
                    AlertType = "danger",
                    ShowAlertCloseButton = true
                };
                break;
            case CreateShortenedLinkResult.VanityAlreadyExists:
                alert = new()
                {
                    AlertContent = serviceResult.ToDescriptionString("Vanity already exists!"),
                    AlertType = "danger",
                    ShowAlertCloseButton = true
                };
                break;
            case CreateShortenedLinkResult.VanityTooLong:
                alert = new()
                {
                    AlertContent = serviceResult.ToDescriptionString("Vanity is too long! (max: 100)"),
                    AlertType = "danger",
                    ShowAlertCloseButton = true
                };
                break;
            case CreateShortenedLinkResult.NotAuthorizedFeatureDisabled:
                if (!HttpContext.IsHtmxRequest())
                {
                    HttpContext.Response.StatusCode = 403;
                }
                return View("NotAuthorized", new NotAuthorizedViewModel()
                {
                    Message = "Link Shortener is disabled."
                });
            case CreateShortenedLinkResult.NotAuthorizedMissingVanityCreatorRole:
                alert = new()
                {
                    AlertContent = "You do not have permission to create Vanity links.",
                    AlertType = "warning",
                    ShowAlertCloseButton = true
                };
                break;
            case CreateShortenedLinkResult.NotAuthorizedNotLoggedIn:
                HttpContext.Response.StatusCode = 403;
                return View("NotAuthorized", new NotAuthorizedViewModel()
                {
                    RequireLogin = true
                });
            default:
                throw new NotImplementedException($"Where {nameof(CreateShortenedLinkResult)}={serviceResult}");
        }

        var vm = await GetViewModel(page);
        if (alertIsForModal)
        {
            vm.CreateModalAlert = alert;
            vm.OpenCreateModal = true;
            vm.CreateModalModel = new()
            {
                Destination = link,
                Vanity = vanity,
                UseVanity = useVanity
            };
        }
        else
        {
            vm.Alert = alert;
        }

        return PartialView("Index", vm);
    }
}