using Kasta.Web.Models.Components;

namespace Kasta.Web.Areas.Admin.Models.Mailbox;

public class MailboxListViewModel
{
    public BaseAlertViewModel? Alert { get; set; }
    public List<MinimalSystemInboxModel> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public bool IsLastPage { get; set; }
}