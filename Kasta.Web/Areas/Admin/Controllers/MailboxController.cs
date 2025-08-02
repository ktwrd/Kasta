using System.Text.Json;
using Kasta.Data;
using Kasta.Web.Areas.Admin.Models.Mailbox;
using Kasta.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("~/Admin/[controller]")]
[Authorize(Roles = $"{RoleKind.Administrator}, {RoleKind.ViewSystemMailbox}")]
public class MailboxController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MailboxController> _logger;
    
    public MailboxController(IServiceProvider services, ILogger<MailboxController> logger)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _logger = logger;
    }

    private async Task<MailboxListViewModel> GetIndexViewModel(
        int page = 1,
        bool showDeleted = false)
    {
        if (page <= 0)
        {
            page = 1;
        }

        var query = _db.SystemMailboxMessages
            .AsNoTracking()
            .Where(e => e.IsDeleted == false)
            .Select(e => new {e.Id, e.Subject, e.Seen, e.CreatedAt});
        
        if (showDeleted)
        {
            query = _db.SystemMailboxMessages
                .AsNoTracking()
                .Where(e => e.IsDeleted == true)
                .Select(e => new {e.Id, e.Subject, e.Seen, e.CreatedAt});
        }

        query = query.OrderByDescending(e => e.CreatedAt);

        var (results, lastPage) = await _db.PaginateAsync(query, page, 25);
        
        var viewModel = new MailboxListViewModel()
        {
            Page = page,
            IsLastPage = lastPage || results.Count <= 0
        };
        foreach (var i in results)
        {
            viewModel.Items.Add(new MinimalSystemInboxModel()
            {
                Id = i.Id,
                CreatedAt = i.CreatedAt,
                Seen = i.Seen,
                Subject = i.Subject
            });
        }

        return viewModel;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] int page = 1,
        [FromQuery] bool showDeleted = false)
    {
        try
        {
            var viewModel = await GetIndexViewModel(page, showDeleted);
            return View("Index", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mailbox list (page: {Page}, showDeleted: {ShowDeleted})", page, showDeleted);
            throw;
        }
    }

    [HttpGet("ViewMessage")]
    public async Task<IActionResult> ViewMessage(
        [FromQuery] string id)
    {
        var model = await _db.SystemMailboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
        if (model == null)
        {
            Response.StatusCode = 404;
            return View("NotFound", new NotFoundViewModel
            {
                Message = $"Could not find message with Id `{id}`",
            });
        }

        if (!model.Seen)
        {
            try
            {
                await using var ctx = _db.CreateSession();
                await using var trans = await ctx.Database.BeginTransactionAsync();
                try
                {
                    await ctx.SystemMailboxMessages.Where(e => e.Id == model.Id)
                        .ExecuteUpdateAsync(e =>
                            e.SetProperty(x => x.Seen, true));
                    await ctx.SaveChangesAsync();
                    await trans.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to mark message with Id {ModelId} as seen", model.Id);
                    await trans.RollbackAsync();
                    throw new ApplicationException($"Failed to mark message with Id {model.Id} as seen.", ex);
                }
                _logger.LogDebug("Marked message {ModelId} as seen", model.Id);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetExtra("FileModel", JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true }));
                    scope.SetTag("FileId", model.Id);
                });
            }
        }

        return View("ViewMessage", model);
    }

    [HttpGet("DeleteMessage")]
    public async Task<IActionResult> DeleteMessage([FromQuery] string id)
    {
        var model = await _db.SystemMailboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
        if (model == null)
        {
            Response.StatusCode = 404;
            return View("NotFound", new NotFoundViewModel
            {
                Message = $"Could not find message with Id `{id}`",
            });
        }

        if (model.IsDeleted)
        {
            Response.StatusCode = 400;
            return View("BadRequest", new BadRequestViewModel
            {
                Message = "Message is already been deleted.",
            });
        }
        
        try
        {
            await using var ctx = _db.CreateSession();
            await using var trans = await ctx.Database.BeginTransactionAsync();
            try
            {
                await ctx.SystemMailboxMessages.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e =>
                        e.SetProperty(x => x.IsDeleted, true));
                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark message with Id {ModelId} as deleted", model.Id);
                await trans.RollbackAsync();
                throw new ApplicationException($"Failed to mark message with Id {model.Id} as deleted.", ex);
            }
            _logger.LogDebug("Marked message {ModelId} as deleted", model.Id);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetExtra("FileModel", JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true }));
                scope.SetTag("FileId", model.Id);
            });
        }
        
        var viewModel = await GetIndexViewModel();
        viewModel.Alert = new()
        {
            AlertType = "success",
            AlertContent = "Message deleted successfully.",
            ShowAlertCloseButton = true
        };
        return View("Index", viewModel);
    }
}