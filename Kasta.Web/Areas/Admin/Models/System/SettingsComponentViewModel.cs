using Kasta.Web.Models;
using Kasta.Web.Models.Components;

namespace Kasta.Web.Areas.Admin.Models.System;

public class SettingsComponentViewModel : BaseAlertViewModel
{
    public SystemSettingsViewModel SystemSettings { get; set; } = new();
}