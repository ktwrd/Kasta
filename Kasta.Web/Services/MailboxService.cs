using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared.Helpers;
using NLog;

namespace Kasta.Web.Services;

public class MailboxService
{
    private readonly ApplicationDbContext _db;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();

    public MailboxService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    public SystemMailboxMessageModel CreateMessage(string subject, string[] body)
    {
        return CreateMessageAsync(subject, string.Join("\n", body)).Result;
    }
    public SystemMailboxMessageModel CreateMessage(string subject, string body)
    {
        return CreateMessageAsync(subject, body).Result;
    }
    
    public Task<SystemMailboxMessageModel> CreateMessageAsync(string subject, string[] body)
    {
        return CreateMessageAsync(subject, string.Join("\n", body));
    }

    public async Task<SystemMailboxMessageModel> CreateMessageAsync(string subject, string body)
    {
        if (body.Length > SystemMailboxMessageModel.MessageMaxLength)
        {
            throw new ArgumentException(
                $"Too long, must be less than {SystemMailboxMessageModel.MessageMaxLength} characters (current length: {body.Length})",
                nameof(body));
        }

        var model = new SystemMailboxMessageModel()
        {
            Subject = subject.FancyMaxLength(SystemMailboxMessageModel.SubjectMaxLength),
            Message = body
        };

        if (subject.Length > SystemMailboxMessageModel.SubjectMaxLength)
        {
            _log.Warn($"Subject truncated to {SystemMailboxMessageModel.SubjectMaxLength} characters (Id: {model.Id})");
        }

        await using (var ctx = _db.CreateSession())
        {
            var trans = await ctx.Database.BeginTransactionAsync();
            try
            {
                await ctx.SystemMailboxMessages.AddAsync(model);
                await ctx.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                _log.Error(ex, $"Failed to create mailbox message.");
                throw;
            }
        }
        _log.Debug($"Created {nameof(SystemMailboxMessageModel)} with Id {model.Id}");

        return model;
    }
}