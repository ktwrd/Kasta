using System.ComponentModel;
using System.Reflection;
using Kasta.Web.Models;
using Kasta.Web.Models.Api.Response;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Kasta.Web;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuthRequiredAttribute : ActionFilterAttribute
{
    [DefaultValue(false)]
    public bool UseJsonResult { get; set; } = false;

    [DefaultValue(true)]
    public bool AllowApiKeyAuth { get; set; } = true;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        context.HttpContext.Request.EnableBuffering();
        if (context.Controller.GetType().GetCustomAttribute<ApiControllerAttribute>() != null)
        {
            UseJsonResult = true;
        }

        var isAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated ?? false;
        var userService = context.HttpContext.RequestServices.GetRequiredService<UserService>();
        if (!isAuthenticated)
        {
            isAuthenticated = userService.IsAuthorized(context.HttpContext, allowApiKey: AllowApiKeyAuth)
                .GetAwaiter().GetResult();
        }

        if (!isAuthenticated)
        {
            context.HttpContext.Response.StatusCode = 401;
            if (UseJsonResult)
            {
                context.Result = new JsonResult(new JsonErrorResponseModel
                {
                    Message = "Not Authorized"
                });
            }
            else
            {
                context.Result = new ViewResult()
                {
                    ViewName = "NotAuthorized",
                    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    {
                        Model = new NotAuthorizedViewModel()
                        {
                            RequireLogin = true
                        }
                    }
                };
            }
            return;
        }
        base.OnActionExecuting(context);
    }
}