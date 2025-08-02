using System.Diagnostics;
using Kasta.Data;
using Kasta.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kasta.Web;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class BlockUserRegisterAttribute() : TypeFilterAttribute(typeof(BlockUserRegisterFilter));

public class BlockUserRegisterFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.HttpContext.Request.Path.HasValue)
        {
            if (context.HttpContext.Request.Path.Value.ToString().StartsWith("/Identity/Account/Register"))
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var settings = db.GetSystemSettings();
                if (!settings.EnableUserRegister)
                {
                    context.Result = new RedirectResult("/Identity/Account/Login");
                }
            }
        }
    }
}