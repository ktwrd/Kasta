using System.ComponentModel;

namespace Kasta.Web.Models;

public class NotAuthorizedViewModel : MessageViewModel
{
    [DefaultValue(false)]
    public bool RequireLogin { get; set; }
}