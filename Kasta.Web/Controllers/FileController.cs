using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Helpers;
using Kasta.Web.Models;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.Controllers;

public class FileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;
    private readonly SignInManager<UserModel> _signInManager;
    private readonly FileService _fileService;

    private readonly ILogger<FileController> _log;

    public FileController(ILogger<FileController> logger, IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
        _signInManager = services.GetRequiredService<SignInManager<UserModel>>();
        _fileService = services.GetRequiredService<FileService>();

        _log = logger;
    }

    [HttpGet("~/d/{id}")]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }
        _log.LogDebug($"Fetching file with requested ID \"{id}\"");
        var file = await _db.GetFileAsync(id);
        if (file == null)
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }
        if (!file.Public)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                Response.StatusCode = 404;
                return View("NotFound");
            }
            var userModel = await _userManager.GetUserAsync(User);

            if ((userModel?.Id ?? "invalid") != file.CreatedByUserId)
            {
                if (!(userModel?.IsAdmin ?? false))
                {
                    Response.StatusCode = 404;
                    return View("NotFound");
                }
            }
        }

        var bot = KastaWebHelper.GetBotFeatures(Request.Headers.UserAgent.ToString());
        if (bot == BotFeature.EmbedMedia)
        {
            return new RedirectResult(Url.Action("GetFile", "ApiFile", new
            {
                value = id,
                preview = false
            })!);
        }

        var systemSettings = _db.GetSystemSettings();
        var vm = new FileDetailViewModel()
        {
            File = file,
            Embed = systemSettings.EnableEmbeds
        };


        if (_fileService.AllowPlaintextPreview(file))
        {
            vm.PreviewContent = _fileService.GetPlaintextPreview(file);
        }

        return View("Details", vm);
    }
}