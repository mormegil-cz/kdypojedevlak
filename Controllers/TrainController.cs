#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using KdyPojedeVlak.Engine;
using KdyPojedeVlak.Engine.Algorithms;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KdyPojedeVlak.Controllers
{
    public class TrainController : Controller
    {
        private static readonly Regex reTrainNumber = new Regex(@"^\s*[A-Z]*\s*(?<id>[0-9]+)\s*$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        private readonly DbModelContext dbModelContext;

        public TrainController(DbModelContext dbModelContext)
        {
            this.dbModelContext = dbModelContext;
        }

        public IActionResult Index(string? search)
        {
            if (String.IsNullOrEmpty(search)) return View();

            var parsed = reTrainNumber.Match(search);
            if (parsed.Success)
            {
                var id = parsed.Groups["id"].Value;

                if (dbModelContext.Trains.Any(t => t.Number == id))
                {
                    return RedirectToAction("Details", new {id});
                }
                else
                {
                    return View((object) $"Vlak č. {id} nebyl nalezen.");
                }
            }
            else
            {
                var trainByName = dbModelContext.TrainTimetables.Include(tt => tt.Train).FirstOrDefault(tt => tt.Name == search);
                if (trainByName != null)
                {
                    return RedirectToAction("Details", new {id = trainByName.TrainNumber});
                }
                else
                {
                    return View((object) "Vlak nebyl nalezen. Zadejte číslo vlaku, případně včetně uvedení typu, např. „12345“, „Os 12345“, „R135“ apod., popř. název vlaku");
                }
            }
        }

        public IActionResult Newest()
        {
            var newestTrains = dbModelContext
                .Set<TrainTimetableVariant>()
                .OrderByDescending(ttv => ttv.ImportedFrom.CreationDate)
                .Where(ttv => ttv.Timetable.Train != null && ttv.Timetable.Train.Number != null)
                .Include(ttv => ttv.Points).ThenInclude(rp => rp.Point)
                .Include(ttv => ttv.Timetable).ThenInclude(tt => tt.Train)
                .Take(10)
                .ToList();
            return View(newestTrains);
        }

        public IActionResult Details(string? id, int? year)
        {
            var plan = BuildTrainPlan(id, year);
            if (plan == null)
            {
                return RedirectToAction("Index", new {search = id});
            }

            return View(plan);
        }

        public IActionResult Map(string? id, int? year)
        {
            id = id?.Trim();
            if (String.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

            var dbYear = GetYear(year);
            if (year == null)
            {
                DebugLog.LogProblem("No year found for {0}", year);
            }

            var train = dbModelContext.Trains.SingleOrDefault(t => t.Number == id);
            if (train == null)
            {
                return RedirectToAction("Index", new {search = id});
            }

            var timetableQuery = dbModelContext.TrainTimetables
                .Include(tt => tt.Variants)
                .ThenInclude(ttv => ttv.Points)
                .ThenInclude(p => p.Point)
                .Include(tt => tt.Variants)
                .ThenInclude(ttv => ttv.Calendar)
                .Where(t => t.Train == train);

            var timetable = timetableQuery.SingleOrDefault(t => t.TimetableYear == dbYear);
            if (timetable == null && year == null)
            {
                timetable = timetableQuery.OrderByDescending(t => t.TimetableYear.Year).FirstOrDefault();
            }
            if (timetable == null)
            {
                return RedirectToAction("Index", new {search = id});
            }

            var pointsInVariants = timetable.Variants.Select(
                variant => variant.Points
                    .OrderBy(p => p.Order)
                    .Select(point => point.Point)
                    .ToList()
            ).ToList();

            var points = new HashSet<RoutingPoint>(pointsInVariants.SelectMany(pl => pl.Where(p => p.Latitude != null))).Select(p => new JObject
            {
                new JProperty("coords", new JArray(p!.Latitude, p!.Longitude)),
                new JProperty("title", p.Name)
            }).Cast<object>().ToArray();
            // TODO: Line titles
            var lines = pointsInVariants.Select(ttv =>
                new JArray(ttv.Where(p => p.Latitude != null).Select(p => new JArray(p!.Latitude, p!.Longitude)).Cast<object>().ToArray())
            ).Cast<object>().ToArray();

            JObject dataJson = new JObject
            {
                new JProperty("lines", new JArray(lines)),
                new JProperty("points", new JArray(points))
            };

            return View(new TrainMapData(timetable, dataJson.ToString(Formatting.None)));
        }

        private TrainPlan? BuildTrainPlan(string? id, int? yearNumber)
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
                .Include(tt => tt.TimetableYear)
                .Include(tt => tt.Variants)
                .ThenInclude(ttv => ttv.Points)
                .ThenInclude(p => p.Point)
                .Include(tt => tt.Variants)
                .ThenInclude(ttv => ttv.Calendar)
                .Where(t => t.Train == train);

            var timetable = timetableQuery.SingleOrDefault(t => t.TimetableYear == year);
            if (timetable == null && yearNumber == null)
            {
                timetable = timetableQuery.OrderByDescending(t => t.TimetableYear.Year).FirstOrDefault();
            }
            if (timetable == null) return null;

            var passagesInVariants = timetable.Variants.Select(
                variant => variant.Points
                    .OrderBy(p => p.Order)
                    .Select(point => point)
                    .ToList()
            ).ToList();
            var pointsInVariants = passagesInVariants.Select(variant => variant.Select(passage => passage.Point).ToList()).ToList();
            var pointList = ListMerger.MergeLists(pointsInVariants);
            var pointIndices = new Dictionary<RoutingPoint, int>(pointList.Count);
            for (var i = 0; i < pointList.Count; ++i)
            {
                if (pointIndices.ContainsKey(pointList[i]))
                {
                    DebugLog.LogProblem("Duplicate point in list: {0}", pointList[i].Name);
                }

                pointIndices[pointList[i]] = i;
            }
            // pointList.Select((point, index) => (point, index)).ToDictionary(tuple => tuple.point, tuple => tuple.index);

            var columns = new List<List<Passage>>(timetable.Variants.Count);
            foreach(var pointsInVariant in passagesInVariants)
            {
                var column = new Passage?[pointList.Count];
                var lastIndex = -1;
                foreach (var point in pointsInVariant)
                {
                    var pointIndex = pointIndices[point.Point];
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
                var variants = new List<Passage>(timetable.Variants.Count);
                for (var j = 0; j < timetable.Variants.Count; ++j)
                {
                    variants.Add(columns[j][i]);
                }

                variantRoutingPoints.Add(variants);
            }

            var pointCount = variantRoutingPoints.Count;
            var majorPointFlags = variantRoutingPoints.Select((point, idx) => idx == 0 || idx == pointCount - 1 || point.Any(variant => variant != null && variant.IsMajorPoint)).ToList();

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

            return new TrainPlan(timetable, pointList, variantRoutingPoints, majorPointFlags, companies, vagonWebCompanyId);
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
}