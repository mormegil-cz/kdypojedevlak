using KdyPojedeVlak.Web.Engine;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KdyPojedeVlak.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult About()
    {
        return View(ScheduleVersionInfo.CurrentVersionInformation);
    }

    public IActionResult Error(int? status = null)
    {
        var statusOrDefault = status ?? 500;
        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature == null)
        {
            DebugLog.LogProblem("Unhandled error {0}", status);
        }
        else if (statusOrDefault >= 500)
        {
            DebugLog.LogProblem("Unhandled error {0} at {1}: {2}", status, exceptionHandlerPathFeature.Path, exceptionHandlerPathFeature.Error);
        }
        else
        {
            DebugLog.LogProblem("Unhandled error {0} at {1} ({2})", status, exceptionHandlerPathFeature.Path, exceptionHandlerPathFeature.Error.Message);
        }

        return statusOrDefault switch
        {
            404 => View("Error404"),
            >= 400 and < 500 => View("Error4xx", statusOrDefault),
            500 => View("Error5xx", statusOrDefault),
            _ => View()
        };
    }
}