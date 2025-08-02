using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.EntityFrameworkCore.Query;

namespace Kasta.Data.Models;

public class FileImageInfoModel
{
    public const string TableName = "FileImageInfo";
    public FileImageInfoModel()
    {
        Id = Guid.Empty.ToString();
    }
    /// <summary>
    /// Foreign Key to <see cref="FileModel"/>
    /// </summary>
    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    [AuditIgnore]
    public FileModel File { get; set; }

    /// <summary>
    /// Image width (pixels)
    /// </summary>
    public uint Width { get; set; }
    
    /// <summary>
    /// Image height (pixels)
    /// </summary>
    public uint Height { get; set; }

    [MaxLength(100)]
    public string? ColorSpace { get; set; }

    [MaxLength(100)]
    public string? CompressionMethod { get; set; }
    
    [MaxLength(100)]
    public string? MagickFormat { get; set; }
    
    [MaxLength(100)]
    public string? Interlace { get; set; }
    
    public uint CompressionLevel { get; set; }

    public string FormatString()
    {
        var sb = new StringBuilder();
        sb.Append(Width.ToString());
        sb.Append("x");
        sb.Append(Height.ToString());

        var info = new List<string>();
        if (!string.IsNullOrEmpty(ColorSpace))
        {
            info.Add(ColorSpace);
        }
        if (!string.IsNullOrEmpty(CompressionMethod))
        {
            info.Add($"{CompressionMethod} Compression");
        }
        if (!string.IsNullOrEmpty(MagickFormat))
        {
            info.Add(MagickFormat);
        }
        if (!string.IsNullOrEmpty(Interlace) && Interlace != "NoInterlace")
        {
            info.Add(Interlace);
        }
        if (info.Count > 0)
        {
            var join = string.Join(", ", info);
            sb.Append(" (");
            sb.Append(join);
            sb.Append(')');
        }
        return sb.ToString();
    }
}