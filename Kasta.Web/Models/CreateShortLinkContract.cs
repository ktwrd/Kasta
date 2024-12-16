using System.ComponentModel.DataAnnotations;

namespace Kasta.Web.Models;

public class CreateShortLinkContract
{
    public string? ShortLinkName { get; set; }

    [Required]
    public string Destination { get; set; } = "";
}