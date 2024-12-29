namespace Kasta.Web.Areas.Admin.Models.Mailbox;

public class MinimalSystemInboxModel
{
    public string Id { get; set; } = Guid.Empty.ToString();
    public string Subject { get; set; } = "";
    public bool Seen { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}