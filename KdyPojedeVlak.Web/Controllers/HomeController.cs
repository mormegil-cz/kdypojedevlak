using System.Linq;
using KdyPojedeVlak.Web.Engine;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace KdyPojedeVlak.Controllers
{
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
}