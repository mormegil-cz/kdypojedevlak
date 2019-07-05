using System.Linq;
using KdyPojedeVlak.Engine;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Models;
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