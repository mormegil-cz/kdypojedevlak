using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KdyPojedeVlak.Engine;
using KdyPojedeVlak.Engine.Djr;
using KdyPojedeVlak.Models;
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

            var pointsInVariants = train.RouteVariants.Select(variant => variant.RoutingPoints.Select(point => point.Point).ToList()).ToList();
            var pointList = Algorithms.MergeLists(pointsInVariants);
            var pointIndices = new Dictionary<RoutingPoint, int>(pointList.Count);
            for (var i = 0; i < pointList.Count; ++i)
            {
                if (pointIndices.ContainsKey(pointList[i]))
                {
                    Console.WriteLine("Duplicate point in list: {0}", pointList[i].Name);
                }
                pointIndices[pointList[i]] = i;
            }
            // pointList.Select((point, index) => (point, index)).ToDictionary(tuple => tuple.point, tuple => tuple.index);

            var columns = new List<List<TrainRoutePoint>>(train.RouteVariants.Count);
            foreach (var variant in train.RouteVariants)
            {
                var column = new TrainRoutePoint[pointList.Count];
                var lastIndex = -1;
                foreach (var point in variant.RoutingPoints)
                {
                    var pointIndex = pointIndices[point.Point];
                    if (column[pointIndex] != null)
                    {
                        Console.WriteLine("Point #{0} ({1}) is duplicated after {2}", pointIndex, point.Point.Name, lastIndex);
                        // throw new NotSupportedException("Cannot insert duplicate route point");
                    }
                    if (pointIndex < lastIndex)
                    {
                        Console.WriteLine("Point #{0} ({1}) goes into reverse after {2}", pointIndex, point.Point.Name, lastIndex);
                        //throw new NotSupportedException("Cannot go in reverse");
                    }
                    column[pointIndex] = point;
                    lastIndex = pointIndex;
                }
                columns.Add(column.ToList());
            }

            var variantRoutingPoints = new List<List<TrainRoutePoint>>(pointList.Count);
            for (var i = 0; i < pointList.Count; ++i)
            {
                var variants = new List<TrainRoutePoint>(train.RouteVariants.Count);
                for (var j = 0; j < train.RouteVariants.Count; ++j)
                {
                    variants.Add(columns[j][i]);
                }
                variantRoutingPoints.Add(variants);
            }

            var majorPointFlags = variantRoutingPoints.Select(point => point.Any(variant => variant != null && variant.IsMajorPoint)).ToList();
            majorPointFlags[0] = true;

            return View(new TrainPlan(train, pointList, variantRoutingPoints, majorPointFlags));
        }
    }
}