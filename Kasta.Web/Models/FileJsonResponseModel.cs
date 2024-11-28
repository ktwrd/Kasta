namespace Kasta.Web.Models;

public class FileJsonResponseModel
{
    public string Id { get; set; }
    public string Url { get; set; }
    public string Filename { get; set; }
    public long FileSize { get; set; }
    public long CreatedAtTimestamp { get; set; }
}