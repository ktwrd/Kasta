using System.ComponentModel;
using System.Reflection;
using kate.FileShare.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace kate.FileShare;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuthRequiredAttribute : ActionFilterAttribute
{
    [DefaultValue(false)]
    public bool UseJsonResult { get; set; } = false;
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        context.HttpContext.Request.EnableBuffering();
        if (context.Controller.GetType().GetCustomAttribute<ApiControllerAttribute>() != null)
        {
            UseJsonResult = true;
        }

        if (!(context.HttpContext.User.Identity?.IsAuthenticated ?? false))
        {
            context.HttpContext.Response.StatusCode = 401;
            if (UseJsonResult)
            {
                context.Result = new JsonResult(new JsonErrorResponseModel()
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