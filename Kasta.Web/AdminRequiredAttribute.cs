using System.ComponentModel;
using System.Reflection;
using Kasta.Data.Models;
using Kasta.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Kasta.Web;

/// <summary>
/// When applied to a class or method, only users that have <see cref="UserModel.IsAdmin"/> set to <see langword="true"/> will be able to access it.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AdminRequiredAttribute : ActionFilterAttribute
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

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<UserModel>>();
        var user = userManager.GetUserAsync(context.HttpContext.User).Result;
        if (!(user?.IsAdmin ?? false))
        {
            var vm = new NotAuthorizedViewModel()
            {
                Message = "You are not an admin.",
                RequireLogin = false
            };
            context.HttpContext.Response.StatusCode = 403;
            if (UseJsonResult)
            {
                context.Result = new JsonResult(new JsonErrorResponseModel()
                {
                    Message = vm.Message
                });
            }
            else
            {
                context.Result = new ViewResult()
                {
                    ViewName = "NotAuthorized",
                    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    {
                        Model = vm
                    }
                };
            }
            return;
        }
        base.OnActionExecuting(context);
    }
}