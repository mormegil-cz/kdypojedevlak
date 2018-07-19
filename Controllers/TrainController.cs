using System;
using System.Text.RegularExpressions;
using KdyPojedeVlak.Engine.Djr;
using Microsoft.AspNetCore.Mvc;

namespace KdyPojedeVlak.Controllers
{
    public class TrainController : Controller
    {
        private static readonly Regex reTrainNumber = new Regex(@"^\s*[A-Z]*\s*(?<id>[0-9]+)\s*$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        public IActionResult Index(string search)
        {
            if (String.IsNullOrEmpty(search)) return View();

            var parsed = reTrainNumber.Match(search);
            if (!parsed.Success) return View((object) "Nesmyslné zadání. Zadejte číslo vlaku, případně včetně uvedení typu, např. „12345“, „Os 12345“, „R135“ apod.");
            var id = parsed.Groups["id"].Value;
            if (String.IsNullOrEmpty(id)) return View((object) "Zadejte číslo vlaku, případně včetně uvedení typu, např. „12345“, „Os 12345“, „R135“ apod.");

            Train train;
            if (Program.Schedule.Trains.TryGetValue(id, out train))
            {
                return RedirectToAction("Details", new { id });
            }
            else
            {
                return View((object) String.Format("Vlak č. {0} nebyl nalezen.", id));
            }
        }

        public IActionResult Details(string id)
        {
            id = id?.Trim();
            if (String.IsNullOrEmpty(id)) return RedirectToAction("Index");

            Train train;
            if (!Program.Schedule.Trains.TryGetValue(id, out train))
            {
                return RedirectToAction("Index", new { search = id });
            }
            return View(train);
        }
    }
}