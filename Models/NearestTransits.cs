#nullable enable

using System;
using System.Collections.Generic;
using KdyPojedeVlak.Engine.DbStorage;

namespace KdyPojedeVlak.Models
{
    public class NearestTransits
    {
        public RoutingPoint Point { get; }
        public DateTime StartDate { get; }
        public IEnumerable<Passage> Transits { get; }

        public HashSet<RoutingPoint> NeighboringPoints { get; }
        public List<RoutingPoint>? NearestPoints { get; }

        public NearestTransits(RoutingPoint point, DateTime startDate, IEnumerable<Passage> transits, HashSet<RoutingPoint> neighboringPoints, List<RoutingPoint>? nearestPoints)
        {
            Point = point;
            Transits = transits;
            NeighboringPoints = neighboringPoints;
            NearestPoints = nearestPoints == null || nearestPoints.Count == 0 ? null : nearestPoints;
            StartDate = startDate;
        }
    }
}
