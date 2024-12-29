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

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] int page = 1,
        [FromQuery] bool showDeleted = false)
    {
        if (page <= 0)
        {
            page = 1;
        }

        var query = _db.SystemMailboxMessages.Where(e => e.IsDeleted == false)
            .Select(e => new {e.Id, e.Subject, e.Seen, e.CreatedAt});
        
        if (showDeleted)
        {
            query = _db.SystemMailboxMessages.Where(e => e.IsDeleted == true)
                .Select(e => new {e.Id, e.Subject, e.Seen, e.CreatedAt});
        }

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

        return View("Index", viewModel);
    }

    [HttpGet("ViewMessage")]
    public async Task<IActionResult> ViewMessage(
        [FromQuery] string id)
    {
        var model = await _db.SystemMailboxMessages.Where(e => e.Id == id).FirstOrDefaultAsync();
        if (model == null)
        {
            Response.StatusCode = 404;
            return View("NotFound", new NotFoundViewModel()
            {
                Message = $"Could not find message with Id `{id}`",
            });
        }

        if (!model.Seen)
        {
            try
            {
                await using var ctx = _db.CreateSession();
                var trans = await ctx.Database.BeginTransactionAsync();
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
                    _logger.LogError(ex, $"Failed to mark message with Id {model.Id} as seen");
                    await trans.RollbackAsync();
                    throw new ApplicationException($"Failed to mark message with Id {model.Id} as seen.", ex);
                }
                _logger.LogDebug($"Marked message {model.Id} as seen.");
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
}