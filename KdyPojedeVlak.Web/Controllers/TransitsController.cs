#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace KdyPojedeVlak.Web.Controllers;

public class TransitsController(DbModelContext dbModelContext) : Controller
{
    private static readonly IList<KeyValuePair<string, string>> emptyPointList = Array.Empty<KeyValuePair<string, string>>();

    private static readonly int[] intervals = [1, 3, 5, 10, 15, 30, 60, 120, 240, 300, 480, 720, 1440];
    private const int GoodMinimum = 4;
    private const int GoodEnough = 7;
    private const int AbsoluteMaximum = 40;

    public IActionResult Index()
    {
        return RedirectToAction("ChoosePoint");
    }

    public IActionResult ChoosePoint(string? search)
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

    public IActionResult GeoLocateMe(float? lat, float? lon, string? embed)
    {
        if (lat == null || lon == null) return RedirectToAction("ChoosePoint");

        var nearestPoints = Program.PointCodebook.FindNearest(lat.GetValueOrDefault(), lon.GetValueOrDefault(), 10)
            .Where(p => dbModelContext.RoutingPoints.SingleOrDefault(rp => rp.Code == p.FullIdentifier) != null)
            .ToList();
        return View(!String.IsNullOrEmpty(embed) ? "NearestPointsEmbed" : "NearestPoints", nearestPoints);
    }

    public IActionResult Nearest(string id, DateTime? at)
    {
        if (String.IsNullOrEmpty(id))
        {
            return RedirectToAction("ChoosePoint");
        }

        // TODO: Support UIC identifiers of other countries
        if (id.StartsWith("54") && id.Length == 7)
        {
            return RedirectToAction("Nearest", new { id = "CZ:" + id[2..], at });
        }

        var point = dbModelContext.RoutingPoints.SingleOrDefault(p => p.Code == id);

        if (point == null)
        {
            // TODO: Error message?
            return NotFound();
        }

        var now = DateTime.Now;
        var startDate = at ?? now;
        var neighbors = dbModelContext.GetNeighboringPoints(point);
        var currentTimetableYear = dbModelContext.TimetableYears.SingleOrDefault(y => y.MinDate <= startDate && y.MaxDate >= startDate);
        if (currentTimetableYear == null)
        {
            // ??
            // TODO: Error message?
            return NotFound();
        }

        var passingTrainsQuery = dbModelContext.Entry(point).Collection(p => p.PassingTrains).Query().Where(p => p.TrainTimetableVariant.TimetableYear == currentTimetableYear);
        var trainList = GetTrainList(startDate, passingTrainsQuery);

        return View(new NearestTransits(point, startDate, currentTimetableYear.Year, trainList, neighbors, GetNearPoints(point, neighbors)));
    }

    private static List<NearestTransits.Transit> GetTrainList(DateTime now, IQueryable<Passage> passingTrainsCollection)
    {
        var allPassingTrains = passingTrainsCollection
            .GroupBy(t => t.TrainTimetableVariant.Timetable.Id)
            .Select(g => g
                .Where(p => p.TrainTimetableVariant.Calendar.EndDate >= now)
                .OrderByDescending(p => p.TrainTimetableVariant.ImportedFrom.CreationDate)
                .Select(
                    p => new NearestTransits.Transit(
                        p.TrainTimetableVariant.Calendar.TimetableYearYear,
                        p.TrainTimetableVariant.Calendar,
                        p.ArrivalTime,
                        p.DepartureTime,
                        p.TrainTimetableVariant.Timetable.TrainCategory,
                        p.TrainTimetableVariant.Timetable.Train.Number,
                        p.TrainTimetableVariant.Timetable.Name,
                        p.SubsidiaryLocationDescription,
                        p.TrainTimetableVariant.Points.Where(np => np.Order == p.Order - 1).Select(np => np.Point.Name).SingleOrDefault(),
                        p.TrainTimetableVariant.Points.Where(np => np.Order == p.Order + 1).Select(np => np.Point.Name).SingleOrDefault()
                    )
                )
                .FirstOrDefault()
            )
            .AsEnumerable()
            .Where(t => t != null)
            .Cast<NearestTransits.Transit>()
            .OrderBy(t => t.AnyScheduledTimeOfDay)
            .ToList();
        var passingTrains = allPassingTrains
            .Select(t => (Day: 0, Transit: t)).Concat(allPassingTrains.Select(t => (Day: 1, Transit: t)))
            .Where(p => CheckInCalendar(p.Transit.Calendar, now.Date, p.Day + (p.Transit.AnyScheduledTime?.Days ?? 0)))
            .ToList();

        var nowTime = now.TimeOfDay;
        List<NearestTransits.Transit>? bestList = null;
        var bestOverMinimum = false;
        foreach (var intervalWidth in intervals)
        {
            var startTime = now.TimeOfDay.Add(TimeSpan.FromMinutes(-intervalWidth));
            var endTime = now.TimeOfDay.Add(TimeSpan.FromMinutes(intervalWidth));

            var data = passingTrains
                .Where(p => p.Transit.AnyScheduledTimeOfDay != null)
                .SkipWhile(p => p.Day == 0 && p.Transit.AnyScheduledTimeOfDay < startTime)
                .TakeWhile(p => p.Transit.AnyScheduledTime?.Add(TimeSpan.FromDays(p.Day)) < endTime)
                .Take(AbsoluteMaximum)
                .ToList();

            if (data.Count == AbsoluteMaximum)
            {
                // too many results…
                return bestOverMinimum ? bestList! : data.Select(t => t.Transit).ToList();
            }

            var futureTrainCount = data.Count(pt => pt.Day > 0 || pt.Transit.AnyScheduledTimeOfDay >= nowTime);

            bestList = data.Select(t => t.Transit).ToList();

            if (bestList.Count >= GoodEnough && futureTrainCount >= GoodEnough / 3)
            {
                return bestList;
            }

            bestOverMinimum = bestList.Count >= GoodMinimum && futureTrainCount >= GoodMinimum / 3;
        }

        return bestList!;
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

    private List<RoutingPoint>? GetNearPoints(RoutingPoint fromPoint, HashSet<RoutingPoint> neighbors)
    {
        if (fromPoint.Longitude == null) return null;
        var neighborCodes = neighbors.Select(p => p.Code).ToHashSet();
        neighborCodes.Add(fromPoint.Code);
        var nearestPoints = Program.PointCodebook.FindNearest(fromPoint.Latitude.GetValueOrDefault(), fromPoint.Longitude.GetValueOrDefault(), 6);
        var nearestPointCodes = nearestPoints
            .Select(np => np.FullIdentifier)
            .Where(id => !neighborCodes.Contains(id))
            .ToList();
        return dbModelContext.RoutingPoints.Where(
            rp => nearestPointCodes.Contains(rp.Code)
        ).ToList();
    }
}