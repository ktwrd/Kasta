using Kasta.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.Models.Components;

public class FileListItemComponentViewModel
{
    public required FileModel File { get; set; }
    public required UserSettingModel UserSettings { get; set; }

    public virtual string DeleteFileUrl(IUrlHelper url, FileModel file, string targetReturnUrl)
    {
        return GenerateAction(url, "DeleteFile", "Home", new()
        {
            {"id", file.Id},
            {"returnUrl", targetReturnUrl}
        });
    }
    public virtual string ChangeFileStateUrl(IUrlHelper url, FileModel file)
    {
        return GenerateAction(url, "ChangeFilePublicState", "Home", new()
        {
            {"id", file.Id},
            {"state", (!file.Public).ToString()}
        });
    }
    public string GenerateAction(IUrlHelper url, string action, string controller, Dictionary<string, object> parameters)
    {
        var u = url.Action(action, controller, parameters);
        if (!string.IsNullOrEmpty(u)) return u;
        
        var j = string.Join(", ", parameters.Select(e => $"{e.Key}, {e.Value}"));
        var js = string.IsNullOrEmpty(j) ? "" : $" ({j})";
        throw new InvalidDataException($"Failed to generate Action Url for {nameof(controller)}={controller}, {nameof(action)}={action}" + js);
    }
}