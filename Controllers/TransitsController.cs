using System;
using System.Collections.Generic;
using System.Linq;
using KdyPojedeVlak.Engine;
using KdyPojedeVlak.Models;
using Microsoft.AspNetCore.Mvc;

namespace KdyPojedeVlak.Controllers
{
    public class TransitsController : Controller
    {
        private static readonly IList<KeyValuePair<string, string>> emptyPointList = new KeyValuePair<string, string>[0];
        public IActionResult Index()
        {
            return RedirectToAction("ChoosePoint");
        }

        public IActionResult ChoosePoint(string search)
        {
            if (String.IsNullOrEmpty(search)) return View(emptyPointList);

            // TODO: Proper (indexed) search
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

            RoutingPoint point;
            if (!Program.Schedule.Points.TryGetValue(id, out point))
            {
                // TODO: Error message?
                return NotFound();
            }

            // TODO: dynamic selection of previous trains
            var now = at ?? DateTime.Now;
            var start = now.AddMinutes(-15);
            var nowTime = now.TimeOfDay;
            var startTime = start.TimeOfDay;

            var data = point.PassingTrains.Select(t => new { Day = 0, Train = t }).Concat(point.PassingTrains.Select(t => new { Day = 1, Train = t }))
                .SkipWhile(p => p.Train.AnyScheduledTime < startTime)
                .Where(p => CheckInBitmap(p.Train.Calendar.Bitmap, now, p.Day + p.Train.AnyScheduledTime.Days))
                .TakeWhile((pt, idx) => idx < 5 || (pt.Train.AnyScheduledTime < nowTime && pt.Day == 0));

            return View(new NearestTransits(point, now, data.Select(t => t.Train)));
        }

        private static bool CheckInBitmap(bool[] bitmap, DateTime day, int dayOffset)
        {
            if (bitmap == null) return true;
            var offset = (int)day.AddDays(-dayOffset).Subtract(KangoSchedule.BitmapBaseDate).TotalDays;
            if (offset < 0 || offset >= bitmap.Length) return false;
            return bitmap[offset];
        }
    }
}
