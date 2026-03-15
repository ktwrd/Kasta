namespace Kasta.Web.Models;

public class LinkShortenerCreateModalModel
{
    public string? Destination { get; set; }
    public bool UseVanity { get; set; }
    public string? Vanity { get; set; }
}
