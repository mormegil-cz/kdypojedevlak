using System;
using System.Collections.Generic;
using KdyPojedeVlak.Engine;

namespace KdyPojedeVlak.Models
{
    public class NearestTransits
    {
        public RoutingPoint Point { get; }
        public DateTime StartDate { get; }
        public IEnumerable<TrainRoutePoint> Transits { get; }

        public NearestTransits(RoutingPoint point, DateTime startDate, IEnumerable<TrainRoutePoint> transits)
        {
            Point = point;
            Transits = transits;
            StartDate = startDate;
        }
    }
}
