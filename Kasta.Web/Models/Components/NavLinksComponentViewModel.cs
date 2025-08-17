namespace Kasta.Web.Models.Components;

public class NavLinksComponentViewModel
{
    public required SystemSettingsParams SystemSettings { get; set; }
    public bool ShowMailbox { get; set; }
    public bool ShowSystemAdmin { get; set; }
    public bool ShowUserAdmin { get; set; }
    
    public string? CurrentItem { get; set; }
    public string? AppTitle { get; set; }

    public class NavLinkItem
    {
        public required string Identifier { get; set; }
        public required string Link { get; set; }
        public bool Current { get; set; }
        public string? Icon { get; set; }
        public required string Text { get; set; }
        public bool Visible { get; set; } = true;
    }
}