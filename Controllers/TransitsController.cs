﻿using System;
using System.Collections.Generic;
using System.Linq;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoutingPoint = KdyPojedeVlak.Engine.DbStorage.RoutingPoint;

namespace KdyPojedeVlak.Controllers
{
    public class TransitsController : Controller
    {
        private static readonly IList<KeyValuePair<string, string>> emptyPointList = new KeyValuePair<string, string>[0];

        private static readonly int[] intervals = { 1, 3, 5, 10, 15, 30, 60, 120, 240, 300, 480, 720, 1440 };
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
                .OrderBy(p => p.Name)
                .Select(p => new { p.Code, p.Name })
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

            var point = dbModelContext.RoutingPoints
                .Include(p => p.PassingTrains)
                .ThenInclude(pt => pt.TrainTimetableVariant)
                .ThenInclude(ttv => ttv.Calendar)
                .Include(p => p.PassingTrains)
                .ThenInclude(pt => pt.TrainTimetableVariant)
                .ThenInclude(ttv => ttv.Timetable)
                .ThenInclude(tt => tt.Train)
                .Include(p => p.PassingTrains)
                .ThenInclude(pt => pt.TrainTimetableVariant)
                .ThenInclude(ttv => ttv.Points)
                .ThenInclude(p => p.Point)
                .SingleOrDefault(p => p.Code == id);

            if (point == null)
            {
                // TODO: Error message?
                return NotFound();
            }

            var now = at ?? DateTime.Now;
            var neighbors = dbModelContext.GetNeighboringPoints(point);
            return View(new NearestTransits(point, now, GetTrainList(now, point), neighbors, GetNearPoints(point, neighbors)));
        }

        private List<Passage> GetTrainList(DateTime now, RoutingPoint point)
        {
            var nowTime = now.TimeOfDay;
            List<Passage> bestList = null;
            bool bestOverMinimum = false;

            var allPassingTrains = point.PassingTrains.AsEnumerable().OrderBy(p => p.AnyScheduledTimeOfDay).ToList();
            var passingTrains = allPassingTrains
                .Select(t => (Day: 0, Train: t)).Concat(allPassingTrains.Select(t => (Day: 1, Train: t)))
                .Where(p => CheckInCalendar(p.Train.TrainTimetableVariant.Calendar, now.Date, p.Day + (p.Train.AnyScheduledTime?.Days ?? 0)))
                .ToList();

            for (var i = 0; i < intervals.Length; ++i)
            {
                var intervalWidth = intervals[i];
                var startTime = now.TimeOfDay.Add(TimeSpan.FromMinutes(-intervalWidth));
                var endTime = now.TimeOfDay.Add(TimeSpan.FromMinutes(intervals[i]));

                var data = passingTrains
                    .SkipWhile(p => p.Day == 0 && p.Train.AnyScheduledTimeOfDay < startTime)
                    .TakeWhile(pt => pt.Train.AnyScheduledTime?.Add(TimeSpan.FromDays(pt.Day)) < endTime)
                    .Take(AbsoluteMaximum)
                    .ToList();

                if (data.Count == AbsoluteMaximum)
                {
                    // too many results…
                    return bestOverMinimum ? bestList : data.Select(t => t.Train).ToList();
                }

                var futureTrainCount = data.Count(pt => pt.Day > 0 || pt.Train.AnyScheduledTimeOfDay >= nowTime);

                bestList = data.Select(t => t.Train).ToList();

                if (bestList.Count >= GoodEnough && futureTrainCount >= GoodEnough / 3)
                {
                    return bestList;
                }

                bestOverMinimum = bestList.Count >= GoodMinimum && futureTrainCount >= GoodMinimum / 3;
            }

            return bestList;
        }

        private static bool CheckInCalendar(CalendarDefinition calendar, DateTime day, int dayOffset)
        {
            if (calendar.StartDate > day) return false;
            if (calendar.EndDate.Year > 1 && calendar.EndDate < day) return false;
            var bitmap = calendar.Bitmap;
            if (bitmap == null) return true;

            var offset = (int) day.AddDays(-dayOffset).Subtract(calendar.StartDate).TotalDays;
            if (offset < 0 || offset >= bitmap.Length) return false;
            return bitmap[offset];
        }

        private List<RoutingPoint> GetNearPoints(RoutingPoint fromPoint, HashSet<RoutingPoint> neighbors)
        {
            if (fromPoint.Longitude == null) return null;
            var neighborCodes = neighbors.Select(p => p.Code).ToHashSet();
            neighborCodes.Add(fromPoint.Code);
            var nearestPoints = Program.PointCodebook.FindNearest(fromPoint.Latitude.GetValueOrDefault(), fromPoint.Longitude.GetValueOrDefault(), 6);
            return nearestPoints
                .Select(np => np.FullIdentifier)
                .Where(id => !neighborCodes.Contains(id))
                .Select(id => dbModelContext.RoutingPoints.SingleOrDefault(rp => rp.Code == id))
                .Where(p => p != null)
                .ToList();
        }
    }
}