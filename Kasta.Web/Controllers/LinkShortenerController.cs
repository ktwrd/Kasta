using Kasta.Data;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.Controllers;

[Authorize]
[Route("~/[controller]")]
public class LinkShortenerController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly LinkShortenerWebService _linkShortenerWebService;
    private readonly ILogger<LinkShortenerController> _logger;

    public LinkShortenerController(IServiceProvider services, ILogger<LinkShortenerController> logger)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _linkShortenerWebService = services.GetRequiredService<LinkShortenerWebService>();

        _logger = logger;
    }
}