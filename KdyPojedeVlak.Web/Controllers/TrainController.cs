﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KdyPojedeVlak.Web.Engine;
using KdyPojedeVlak.Web.Engine.Algorithms;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KdyPojedeVlak.Web.Controllers;

public partial class TrainController(DbModelContext dbModelContext) : Controller
{
    [GeneratedRegex(@"^\s*[A-Z]*\s*(?<id>[0-9]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant)]
    private static partial Regex RegexTrainNumber();

    private static readonly Regex reTrainNumber = RegexTrainNumber();

    public IActionResult Index(string? search, int? year)
    {
        if (String.IsNullOrEmpty(search)) return View();

        var parsed = reTrainNumber.Match(search);
        return parsed.Success
            ? SearchByTrainNumber(parsed.Groups["id"].Value, year)
            : SearchByTrainName(search, year);
    }

    private IActionResult SearchByTrainName(string search, int? yearOpt)
    {
        if (yearOpt == null)
        {
            var newestTrainByName = dbModelContext
                .TrainTimetables
                .Include(tt => tt.Train)
                .Where(tt => tt.Name == search)
                .OrderByDescending(tt => tt.YearId)
                .FirstOrDefault();

            if (newestTrainByName == null)
            {
                return View((object) "Vlak nebyl nalezen. Zadejte číslo vlaku, případně včetně uvedení typu, např. „12345“, „Os 12345“, „R135“ apod., popř. název vlaku");
            }

            return RedirectToAction("Details", new { id = newestTrainByName.TrainNumber, year = newestTrainByName.YearId });
        }

        var year = yearOpt.GetValueOrDefault();
        var trainByName = dbModelContext
            .TrainTimetables
            .Include(tt => tt.Train)
            .FirstOrDefault(tt => tt.Name == search && tt.YearId == year);
        if (trainByName != null)
        {
            return RedirectToAction("Details", new { id = trainByName.TrainNumber });
        }
        else
        {
            return View((object) $"Vlak nebyl pro JŘ {year} nalezen. Zadejte číslo vlaku, případně včetně uvedení typu, např. „12345“, „Os 12345“, „R135“ apod., popř. název vlaku");
        }
    }

    private IActionResult SearchByTrainNumber(string id, int? yearOpt)
    {
        if (yearOpt == null)
        {
            var newestTrain = dbModelContext.TrainTimetables.Where(tt => tt.Train.Number == id).OrderByDescending(tt => tt.YearId).FirstOrDefault();
            if (newestTrain == null)
            {
                return View((object) $"Vlak č. {id} nebyl nalezen.");
            }
            return RedirectToAction("Details", new { id, year = newestTrain.YearId });
        }

        var year = yearOpt.GetValueOrDefault();
        if (dbModelContext.TrainTimetables.Any(tt => tt.Train.Number == id && tt.YearId == year))
        {
            return RedirectToAction("Details", new { id, year });
        }
        else
        {
            return View((object) $"Vlak č. {id} nebyl pro JŘ {year} nalezen.");
        }
    }

    public IActionResult Newest()
    {
        const int limit = 10;

        var newestTrains = dbModelContext
            .Set<TrainTimetableVariant>()
            .Where(ttv => dbModelContext.ImportedFiles.OrderByDescending(f => f.CreationDate).Take(limit).Contains(ttv.ImportedFrom))
            .OrderByDescending(ttv => ttv.ImportedFrom.CreationDate)
            .Select(
                ttv =>
                    new BasicTrainInfo(
                        ttv.Calendar.TimetableYearYear,
                        ttv.Timetable.Train.Number,
                        ttv.Timetable.Name,
                        ttv.Points.OrderBy(np => np.Order).Select(np => np.Point.Name).FirstOrDefault(),
                        ttv.Points.OrderByDescending(np => np.Order).Select(np => np.Point.Name).FirstOrDefault(),
                        ttv.Timetable.TrainCategory,
                        ttv.Timetable.TrafficType
                    )
            )
            .Take(limit)
            .ToList();

        return View(newestTrains);
    }

    public IActionResult Details(string? id, int? year, bool? everything)
    {
        var plan = BuildTrainPlan(id, year, everything ?? false);
        return plan == null
            ? RedirectToAction("Index", new { search = id, year })
            : View(plan);
    }

    public IActionResult Map(string? id, int? year)
    {
        id = id?.Trim();
        if (String.IsNullOrEmpty(id))
        {
            return RedirectToAction("Index");
        }

        var dbYear = GetYear(year);
        if (dbYear == null)
        {
            DebugLog.LogProblem("No year found for {0}", year);
        }

        var train = dbModelContext.Trains.SingleOrDefault(t => t.Number == id);
        if (train == null)
        {
            return RedirectToAction("Index", new { search = id, year });
        }

        var timetableQuery = dbModelContext.TrainTimetables
            .Include(tt => tt.Variants)
            .ThenInclude(ttv => ttv.Points)
            .ThenInclude(p => p.Point)
            .Include(tt => tt.Variants)
            .ThenInclude(ttv => ttv.Calendar)
            .Where(t => t.Train == train);

        var timetable = timetableQuery.AsSplitQuery().SingleOrDefault(t => t.TimetableYear == dbYear);
        if (timetable == null)
        {
            return RedirectToAction("Index", new { search = id, year });
        }

        var pointsInVariants = timetable.Variants.Select(
            variant => variant.Points
                .OrderBy(p => p.Order)
                .Select(point => point.Point)
                .ToList()
        ).ToList();

        var points = new HashSet<RoutingPoint>(pointsInVariants.SelectMany(pl => pl.Where(p => p.Latitude != null))).Select(p => new JObject
        {
            new JProperty("coords", new JArray(p!.Latitude!, p!.Longitude!)),
            new JProperty("title", p.Name)
        }).Cast<object>().ToArray();
        // TODO: Line titles
        var lines = pointsInVariants.Select(ttv =>
            new JArray(ttv.Where(p => p.Latitude != null).Select(p => new JArray(p!.Latitude!, p!.Longitude!)).Cast<object>().ToArray())
        ).Cast<object>().ToArray();

        JObject dataJson = new JObject
        {
            new JProperty("lines", new JArray(lines)),
            new JProperty("points", new JArray(points))
        };

        var companies = timetable.Variants
            .Select(v => v.TrainVariantId.Substring(0, 4))
            .GroupBy(code => code)
            .OrderByDescending(g => g.Count())
            .AsEnumerable()
            .Select(g => Program.CompanyCodebook.Find(g.Key))
            .Where(c => c != null)
            .ToList();

        string? vagonWebCompanyId = null;
        if (companies.Count > 0)
        {
            VagonWebCodes.CompanyCodes.TryGetValue(companies.First().ID, out vagonWebCompanyId);
        }

        return View(new TrainMapData(timetable, dataJson.ToString(Formatting.None), companies, vagonWebCompanyId));
    }

    private TrainPlan? BuildTrainPlan(string? id, int? yearNumber, bool everything)
    {
        id = id?.Trim();
        if (String.IsNullOrEmpty(id)) return null;

        var year = GetYear(yearNumber);
        if (year == null)
        {
            DebugLog.LogProblem("No year found for {0}", year);
        }

        var train = dbModelContext.Trains.SingleOrDefault(t => t.Number == id);
        if (train == null) return null;

        var timetableQuery = dbModelContext.TrainTimetables
            .Include(tt => tt.Variants)
            .ThenInclude(ttv => ttv.ImportedFrom)
            .Include(tt => tt.TimetableYear)
            .Include(tt => tt.Variants)
            .ThenInclude(ttv => ttv.Points)
            .ThenInclude(p => p.Point)
            .Include(tt => tt.Variants)
            .ThenInclude(ttv => ttv.Points)
            .ThenInclude(p => p.NetworkSpecificParameters)
            .ThenInclude(nsp => nsp.ValueIndirect)
            .Include(tt => tt.Variants)
            .ThenInclude(ttv => ttv.Calendar)
            .Include(tt => tt.Variants)
            .ThenInclude(ttv => ttv.PttNotes)
            .ThenInclude(n => ((NonCentralPttNoteForVariant) n).Text)
            .Include(tt => tt.Variants)
            .Include(tt => tt.Variants)
            .ThenInclude(ttv => ttv.Cancellations)
            .ThenInclude(c => c.Calendar)
            .Where(t => t.Train == train);

        var timetable = timetableQuery.AsSplitQuery().SingleOrDefault(t => t.TimetableYear == year);
        if (timetable == null) return null;

        var timetableVariants = timetable.Variants;
        var filteredVariants = timetableVariants
            .Where(v => v.Calendar.EndDate >= DateTime.Now)
            .ToList();

        bool isFiltered, canFilter;
        if (everything || filteredVariants.Count == 0)
        {
            isFiltered = false;
            canFilter = timetableVariants.Count > filteredVariants.Count && filteredVariants.Count > 0;
        }
        else
        {
            isFiltered = timetableVariants.Count > filteredVariants.Count;
            canFilter = false;
            timetableVariants = filteredVariants;
        }

        var passagesInVariants = timetableVariants
            .OrderBy(variant => variant.ImportedFrom.CreationDate)
            .Select(
                variant => variant.Points
                    .OrderBy(p => p.Order)
                    .Select(point => point)
                    .ToList()
            ).ToList();
        var pointsInVariants = passagesInVariants.Select(variant => variant.Select(passage => passage.Point).ToList()).ToList();
        var pointList = ListMerger.MergeLists(pointsInVariants);
        var pointIndices = pointList
            .Select(((point, index) => new { point, index }))
            .GroupBy(rp => rp.point)
            .ToDictionary(g => g.Key, g => g.Select(rp => rp.index).ToList());

        var columns = new List<List<Passage>>(passagesInVariants.Count);
        foreach (var pointsInVariant in passagesInVariants)
        {
            var column = new Passage?[pointList.Count];
            var usedPointIndices = new Dictionary<RoutingPoint, int>(pointList.Count);
            var lastIndex = -1;
            foreach (var point in pointsInVariant)
            {
                var pointIndexList = pointIndices[point.Point];
                usedPointIndices.TryGetValue(point.Point, out var currentPointIndex);
                var pointIndex = pointIndexList[currentPointIndex];
                usedPointIndices[point.Point] = currentPointIndex + 1;
                if (column[pointIndex] != null)
                {
                    DebugLog.LogProblem("Point #{0} ({1}) is duplicated after {2}", pointIndex, point.Point.Name, lastIndex);
                    // throw new NotSupportedException("Cannot insert duplicate route point");
                }

                if (pointIndex < lastIndex)
                {
                    DebugLog.LogProblem("Point #{0} ({1}) goes into reverse after {2}", pointIndex, point.Point.Name, lastIndex);
                    //throw new NotSupportedException("Cannot go in reverse");
                }

                column[pointIndex] = point;
                lastIndex = pointIndex;
            }

            columns.Add(column.ToList()!);
        }

        var variantRoutingPoints = new List<List<Passage>>(pointList.Count);
        for (var i = 0; i < pointList.Count; ++i)
        {
            var variants = new List<Passage>(passagesInVariants.Count);
            for (var j = 0; j < passagesInVariants.Count; ++j)
            {
                variants.Add(columns[j][i]);
            }

            variantRoutingPoints.Add(variants);
        }

        var isFirstPoint = true;
        var majorPointFlags = new List<bool>(variantRoutingPoints.Count);
        foreach (var point in variantRoutingPoints)
        {
            majorPointFlags.Add(isFirstPoint || point.Any(variant => variant is { IsMajorPoint: true }));
            if (isFirstPoint && point.Any(variant => variant != null && (variant.ArrivalTime != null || variant.DepartureTime != null))) isFirstPoint = false;
        }
        if (majorPointFlags.Count > 0) majorPointFlags[^1] = true;

        var companies = timetableVariants
            .Select(v => v.TrainVariantId.Substring(0, 4))
            .GroupBy(code => code)
            .OrderByDescending(g => g.Count())
            .AsEnumerable()
            .Select(g => Program.CompanyCodebook.Find(g.Key))
            .Where(c => c != null)
            .ToList();

        string? vagonWebCompanyId = null;
        if (companies.Count > 0)
        {
            VagonWebCodes.CompanyCodes.TryGetValue(companies.First().ID, out vagonWebCompanyId);
        }

        return new TrainPlan(timetable, timetableVariants, pointList, variantRoutingPoints, majorPointFlags, companies, vagonWebCompanyId, isFiltered, canFilter);
    }

    private TimetableYear? GetYear(int? yearNumber)
    {
        if (yearNumber == null)
        {
            var now = DateTime.Now;
            return dbModelContext.TimetableYears.SingleOrDefault(y => y.MinDate <= now && y.MaxDate >= now);
        }
        else
        {
            return dbModelContext.TimetableYears.SingleOrDefault(y => y.Year == yearNumber);
        }
    }
}