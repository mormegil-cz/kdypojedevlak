using System;
using System.Collections.Generic;
using System.Linq;
using KdyPojedeVlak.Engine.DbStorage;
//using KdyPojedeVlak.Engine.Djr;
using KdyPojedeVlak.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KdyPojedeVlak.Controllers
{
    public class TransitsController : Controller
    {
        private static readonly IList<KeyValuePair<string, string>> emptyPointList = new KeyValuePair<string, string>[0];

        private static readonly int[] intervals = {1, 3, 5, 10, 15, 30, 60, 120, 240, 300, 480, 720, 1440};
        private const int GoodMinimum = 4;
        private const int GoodEnough = 7;
        private const int AbsoluteMaximum = 40;

        private readonly DbModelContext dbModelContext;

        public TransitsController(DbModelContext dbModelContext)
        {
            this.dbModelContext = dbModelContext;
        }

        public IActionResult Index()
        {
            return RedirectToAction("ChoosePoint");
        }

        public IActionResult ChoosePoint(string search)
        {
            if (String.IsNullOrEmpty(search)) return View(emptyPointList);

            // TODO: Fulltext search
            var searchResults = dbModelContext.RoutingPoints.Where(p => p.Name.StartsWith(search))
                .Select(p => new {p.Code, p.Name})
                .Take(100)
                .Select(p => new KeyValuePair<string, string>(p.Code, p.Name))
                .ToList();
            // TODO: Proper model
            return View(searchResults.Count == 0 ? null : searchResults);
        }

        public IActionResult Nearest(string id, DateTime? at)
        {
            if (String.IsNullOrEmpty(id))
            {
                return RedirectToAction("ChoosePoint");
            }

            var point = dbModelContext.RoutingPoints.Include(p => p.PassingTrains).SingleOrDefault(p => p.Code == id);
            if (point == null)
            {
                // TODO: Error message?
                return NotFound();
            }

            var now = at ?? DateTime.Now;
            return View(new NearestTransits(point, now, GetTrainList(now, point)));
        }

        private List<Passage> GetTrainList(DateTime now, RoutingPoint point)
        {
            var nowTime = now.TimeOfDay;
            List<Passage> bestList = null;
            bool bestOverMinimum = false;

            for (int i = 0; i < intervals.Length; ++i)
            {
                var intervalWidth = intervals[i];
                var startTime = now.TimeOfDay.Add(TimeSpan.FromMinutes(-intervalWidth));
                var endTime = now.TimeOfDay.Add(TimeSpan.FromMinutes(intervals[i]));

                var passingTrains = point.PassingTrains.ToList();

                var data = passingTrains
                    .Select(t => (Day: 0, Train: t)).Concat(passingTrains.Select(t => (Day: 1, Train: t)))
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