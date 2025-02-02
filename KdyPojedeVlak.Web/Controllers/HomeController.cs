using System.Diagnostics;
using KdyPojedeVlak.Web.Engine;
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
        return (status ?? 0) switch
        {
            404 => View("Error404"),
            >= 400 and < 500 => View("Error4xx", status.GetValueOrDefault()),
            500 => View("Error5xx", status.GetValueOrDefault()),
            _ => View()
        };
    }
}