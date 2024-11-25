using System.ComponentModel;

namespace kate.FileShare.Models;

public class NotAuthorizedViewModel : MessageViewModel
{
    [DefaultValue(false)]
    public bool RequireLogin { get; set; }
}