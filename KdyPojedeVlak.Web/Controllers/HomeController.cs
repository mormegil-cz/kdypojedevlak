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

    public IActionResult Error()
    {
        return View();
    }
}