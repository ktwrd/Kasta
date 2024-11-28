using System.ComponentModel;

namespace Kasta.Data.Models;

public class FilePreviewModel
{
    public const string TableName = "FilePreview";
    public FilePreviewModel()
    {
        Id = Guid.Empty.ToString();
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; set; }
    public FileModel File { get; set; }
    
    /// <summary>
    /// Size of the preview file
    /// </summary>
    public long Size { get; set; }
    /// <summary>
    /// Relative location in the S3 bucket
    /// </summary>
    public string RelativeLocation { get; set; }
    public string Filename { get; set; }
    /// <summary>
    /// Mime type of the file. Should be an image of some sort.
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// When the preview was created at
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}