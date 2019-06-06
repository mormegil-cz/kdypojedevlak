using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KdyPojedeVlak.Engine;
using KdyPojedeVlak.Engine.Algorithms;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Engine.Djr;
using KdyPojedeVlak.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoutingPoint = KdyPojedeVlak.Engine.DbStorage.RoutingPoint;

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

        public IActionResult Index(string search)
        {
            if (String.IsNullOrEmpty(search)) return View();

            var parsed = reTrainNumber.Match(search);
            if (!parsed.Success) return View((object) "Nesmyslné zadání. Zadejte číslo vlaku, případně včetně uvedení typu, např. „12345“, „Os 12345“, „R135“ apod.");
            var id = parsed.Groups["id"].Value;
            if (String.IsNullOrEmpty(id)) return View((object) "Zadejte číslo vlaku, případně včetně uvedení typu, např. „12345“, „Os 12345“, „R135“ apod.");

            if (dbModelContext.Trains.Any(t => t.Number == id))
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
            // TODO: Year
            var year = dbModelContext.TimetableYears.Single();

            id = id?.Trim();
            if (String.IsNullOrEmpty(id)) return RedirectToAction("Index");

            var train = dbModelContext.Trains.SingleOrDefault(t => t.Number == id);
            if (train == null)
            {
                return RedirectToAction("Index", new { search = id });
            }

            var timetable = dbModelContext.TrainTimetables
                .Include(tt => tt.Variants)
                    .ThenInclude(ttv => ttv.Points)
                        .ThenInclude(p => p.Point)
                .Include(tt => tt.Variants)
                    .ThenInclude(ttv => ttv.Calendar)
                .SingleOrDefault(t => t.Train == train && t.TimetableYear == year);

            var pointsInVariants = timetable.Variants.Select(
                variant => variant.Points
                    .OrderBy(p => p.Order)
                    .Select(point => point.Point)
                    .ToList()
            ).ToList();
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
            foreach (var variant in timetable.Variants)
            {
                var column = new Passage[pointList.Count];
                var lastIndex = -1;
                foreach (var point in variant.Points)
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

                columns.Add(column.ToList());
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

            var majorPointFlags = variantRoutingPoints.Select((point, idx) => idx == 0 || point.Any(variant => variant != null && variant.IsMajorPoint)).ToList();

            return View(new TrainPlan(timetable, pointList, variantRoutingPoints, majorPointFlags));
        }
    }
}