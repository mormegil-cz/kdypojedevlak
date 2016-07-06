using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KdyPojedeVlak.Engine;
using Microsoft.AspNetCore.Mvc;

namespace KdyPojedeVlak.Controllers
{
    public class TrainController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(string id)
        {
            id = id?.Trim();
            if (String.IsNullOrEmpty(id)) return RedirectToAction("Index");

            Train train;
            if (!Program.Schedule.Trains.TryGetValue(id, out train))
            {
                return NotFound();
                // TODO: Error message
                //return RedirectToAction("Index");
            }
            return View(train);
        }
    }
}
