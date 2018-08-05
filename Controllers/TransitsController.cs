using System;
using System.Collections.Generic;
using System.Linq;
using KdyPojedeVlak.Engine.Djr;
using KdyPojedeVlak.Models;
using Microsoft.AspNetCore.Mvc;

namespace KdyPojedeVlak.Controllers
{
    public class TransitsController : Controller
    {
        private static readonly IList<KeyValuePair<string, string>> emptyPointList = new KeyValuePair<string, string>[0];

        private static readonly int[] intervals = { 1, 3, 5, 10, 15, 30, 60, 120, 240, 300, 480, 720, 1440 };
        private const int GoodMinimum = 4;
        private const int GoodEnough = 7;
        private const int AbsoluteMaximum = 40;

        public IActionResult Index()
        {
            return RedirectToAction("ChoosePoint");
        }

        public IActionResult ChoosePoint(string search)
        {
            if (String.IsNullOrEmpty(search)) return View(emptyPointList);

            // TODO: Proper (indexed) search
            var searchResults = Program.Schedule.Points
                .Where(p => p.Value.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                            p.Value.ShortName.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                            p.Value.LongName.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .Select(p => new KeyValuePair<string, string>(p.Key, p.Value.LongName))
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

            var now = at ?? DateTime.Now;
            return View(new NearestTransits(point, now, GetTrainList(now, point)));
        }

        private static List<TrainRoutePoint> GetTrainList(DateTime now, RoutingPoint point)
        {
            var nowTime = now.TimeOfDay;
            List<TrainRoutePoint> bestList = null;
            bool bestOverMinimum = false;

            for (int i = 0; i < intervals.Length; ++i)
            {
                var intervalWidth = intervals[i];
                var startTime = now.TimeOfDay.Add(TimeSpan.FromMinutes(-intervalWidth));
                var endTime = now.TimeOfDay.Add(TimeSpan.FromMinutes(intervals[i]));

                var data = point.PassingTrains.Select(t => (Day: 0, Train: t)).Concat(point.PassingTrains.Select(t => (Day: 1, Train: t)))
                    .SkipWhile(p => p.Day == 0 && p.Train.AnyScheduledTime < startTime)
                    .Where(p => CheckInCalendar(p.Train.Calendar, now.Date, p.Day + p.Train.AnyScheduledTimeSpan.Days))
                    .TakeWhile(pt => pt.Train.AnyScheduledTime.Add(TimeSpan.FromDays(pt.Day)) < endTime)
                    .Take(AbsoluteMaximum)
                    .ToList();

                if (data.Count == AbsoluteMaximum)
                {
                    // too many results…
                    return bestOverMinimum ? bestList : data.Select(t => t.Train).ToList();
                }

                var futureTrainCount = data.Count(pt => pt.Day > 0 || pt.Train.AnyScheduledTime >= nowTime);

                bestList = data.Select(t => t.Train).ToList();

                if (bestList.Count >= GoodEnough && futureTrainCount >= GoodEnough / 3)
                {
                    return bestList;
                }

                bestOverMinimum = bestList.Count >= GoodMinimum && futureTrainCount >= GoodMinimum / 3;
            }

            return bestList;
        }

        private static bool CheckInCalendar(TrainCalendar calendar, DateTime day, int dayOffset)
        {
            if (calendar.ValidFrom > day) return false;
            if (calendar.ValidTo.Year > 1 && calendar.ValidTo < day) return false;
            var bitmap = calendar.CalendarBitmap;
            if (bitmap == null) return true;

            var offset = (int) day.AddDays(-dayOffset).Subtract(calendar.BaseDate).TotalDays;
            if (offset < 0 || offset >= bitmap.Length) return false;
            return bitmap[offset];
        }
    }
}