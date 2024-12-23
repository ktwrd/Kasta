using Kasta.Web.Models;

namespace Kasta.Web.Areas.Admin.Models.System;

public class SettingsComponentViewModel : BaseAlertViewModel
{
    public SystemSettingsParams SystemSettings { get; set; } = new();
}