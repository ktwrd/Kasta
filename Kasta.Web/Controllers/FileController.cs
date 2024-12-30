using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Data.Models.Audit;
using Kasta.Web.Helpers;
using Kasta.Web.Models;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>
    /// Update the <see cref="FileModel.ShortUrl"/> property with the <paramref name="vanity"/> parameter.
    /// </summary>
    /// <param name="id"><see cref="FileModel.Id"/> or <see cref="FileModel.ShortUrl"/> to target.</param>
    /// <param name="vanity">New vanity url for <see cref="FileModel.ShortUrl"/></param>
    /// <remarks>
    /// <para>This will only work if the Vanity Url isn't taken, and it's 3 or more characters in length.</para>
    /// 
    /// Requires the <see cref="RoleKind.Administrator"/> or <see cref="RoleKind.FileCreateVanity"/> role.
    /// </remarks>
    [HttpPost("~/File/Details/Update/Vanity")]
    [Authorize(Roles = $"{RoleKind.Administrator}, {RoleKind.FileCreateVanity}")]
    public async Task<IActionResult> SetVanityUrl(
        [FromForm] string id,
        [FromForm] string vanity)
    {
        var scope = _log.BeginScope($"id={id},vanity={vanity}");

        if (string.IsNullOrEmpty(id))
        {
            Response.StatusCode = 404;
            scope?.Dispose();
            return View("NotFound");
        }
        _log.LogDebug($"Fetching file with requested ID \"{id}\"");
        var file = await _db.GetFileAsync(id);
        if (file == null)
        {
            Response.StatusCode = 404;
            scope?.Dispose();
            return View("NotFound");
        }
        if (string.IsNullOrEmpty(vanity))
        {
            Response.StatusCode = 400;
            scope?.Dispose();
            return View("BadRequest", new BadRequestViewModel()
            {
                Message = $"Missing request property \"{nameof(vanity)}\""
            });
        }
        if (vanity.Length < 3)
        {
            Response.StatusCode = 400;
            scope?.Dispose();
            return View("BadRequest", new BadRequestViewModel()
            {
                Message = $"Vanity Url must have 3 or more characters ({vanity})"
            });
        }

        if (await _db.FileExistsAsync(id))
        {
            Response.StatusCode = 400;
            scope?.Dispose();
            return View("BadRequest", new BadRequestViewModel()
            {
                Message = $"Vanity Url already exists ({vanity})"
            });
        }
        var userModel = await _userManager.GetUserAsync(User);
        if (userModel == null)
        {
            throw new InvalidOperationException($"Action has {nameof(AuthorizeAttribute)}, but {nameof(_userManager.GetUserAsync)} returned null?!?!?!?!?!?!?");
        }

        using (var ctx = _db.CreateSession())
        {
            var trans = await ctx.Database.BeginTransactionAsync();
            try
            {
                await ctx.Files
                    .Where(e => e.Id == file.Id)
                    .ExecuteUpdateAsync(m => m.SetProperty(e => e.ShortUrl, vanity));
                
                // auditing
                var auditModel = new AuditModel()
                {
                    CreatedBy = userModel.Id,
                    EntityName = FileModel.TableName,
                    PrimaryKey = file.Id,
                    Kind = AuditEventKind.Update
                };
                await ctx.Audit.AddAsync(auditModel);
                await ctx.AuditEntries.AddAsync(new AuditEntryModel()
                {
                    AuditId = auditModel.Id,
                    PropertyName = nameof(FileModel.ShortUrl),
                    Value = vanity
                });

                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Failed to update record {nameof(FileModel)} (where id={file.Id}) set {nameof(file.ShortUrl)} to \"{id}\"");
                scope?.Dispose();
                await trans.RollbackAsync();
                throw;
            }
        }
        scope?.Dispose();
        return RedirectToAction("Details", new Dictionary<string, object>()
        {
            {"id", id}
        });
    }
}