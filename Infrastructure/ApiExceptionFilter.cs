using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Infrastructure;

/// <summary>
/// Turns any API failure that a controller did not handle itself into a friendly page.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempData;

    public ApiExceptionFilter(ITempDataDictionaryFactory tempData) => _tempData = tempData;

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ApiException api) return;

        var temp = _tempData.GetTempData(context.HttpContext);

        if (api.StatusCode == 401)
        {
            context.HttpContext.Session.Clear();
            temp["Error"] = "Your session has expired. Please log in again.";
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
        else if (api.StatusCode == 403)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
        else
        {
            temp["ErrorMessage"] = api.Message;
            context.Result = new RedirectToActionResult("Error", "Home", new { code = api.StatusCode });
        }

        context.ExceptionHandled = true;
    }
}
