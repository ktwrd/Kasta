using System.Collections.ObjectModel;
using Kasta.Data.Models;
using Kasta.Web.Models;
using Kasta.Web.Models.Components;

namespace Kasta.Web.Areas.Admin.Models.User;

public class EditDetailsComponentViewModel : BaseAlertViewModel
{
    public required string UserId { get; set; }
    public UserLimitModel? Limit { get; set; }
}