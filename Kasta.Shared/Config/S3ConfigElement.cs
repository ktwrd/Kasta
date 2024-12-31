using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class S3ConfigElement
{
    /// <summary>
    /// S3 Service URL (e.g; <c>http://s3.ap-southeast-2.amazonaws.com</c>)
    /// </summary>
    [Required]
    [XmlElement(nameof(ServiceUrl))]
    public string ServiceUrl { get; set; } = "";

    /// <summary>
    /// Access Key Id for the S3 Client
    /// </summary>
    [Required]
    [XmlElement(nameof(AccessKey))]
    public string AccessKey { get; set; } = "";

    /// <summary>
    /// Access Secret Key for the S3 Client
    /// </summary>
    [Required]
    [XmlElement(nameof(AccessSecret))]
    public string AccessSecret { get; set; } = "";
    
    /// <summary>
    /// Name for the S3 Bucket
    /// </summary>
    [Required]
    [XmlElement(nameof(BucketName))]
    public string BucketName { get; set; } = "";

    
    [XmlAttribute("ForcePathStyle")]
    [DefaultValue(false)]
    public bool ForcePathStyle { get; set; } = false;
}