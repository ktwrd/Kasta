namespace Kasta.Web.Models;

[Serializable]
public class SessionCreationStatusResponse
{
    public string FileName { get; set; }
    public string SessionId { get; set; }
    public string UserId { get; set; }
}