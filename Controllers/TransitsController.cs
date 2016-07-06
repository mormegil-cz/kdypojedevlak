using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace KdyPojedeVlak.Controllers
{
    public class TransitsController : Controller
    {
        public IActionResult ChoosePoint(string search)
        {
            if (String.IsNullOrEmpty(search)) return View();

            var searchResults = Program.Schedule.Points
                .Where(p => p.Value.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .Select(p => new KeyValuePair<string, string>(p.Key, p.Value.Name))
                .Take(100)
                .ToList();
            return View(searchResults.Count == 0 ? null : searchResults);
        }

        public IActionResult Nearest(string id, DateTime? at)
        {
            if (String.IsNullOrEmpty(id))
            {
                return RedirectToAction("ChoosePoint");
            }

            var passes = Program.Schedule.GetPassesThrough(id);
            if (passes == null) return NotFound();

            // TODO: dynamic selection of previous trains
            var now = at ?? DateTime.Now;
            var start = now.AddMinutes(-15);
            var nowTime = now.TimeOfDay;
            var startTime = start.TimeOfDay;

            var data = passes
                .SkipWhile(p => p.ScheduledTime < startTime)
                .TakeWhile((pt, idx) => idx < 5 || pt.ScheduledTime < nowTime);

            return View(data);
        }
    }
}
