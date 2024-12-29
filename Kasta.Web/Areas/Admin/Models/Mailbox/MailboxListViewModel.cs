using Kasta.Data.Models;

namespace Kasta.Web.Areas.Admin.Models.Mailbox;

public class MailboxListViewModel
{
    public List<MinimalSystemInboxModel> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public bool IsLastPage { get; set; }
}