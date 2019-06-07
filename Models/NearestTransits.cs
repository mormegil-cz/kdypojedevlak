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

        public NearestTransits(RoutingPoint point, DateTime startDate, IEnumerable<Passage> transits)
        {
            Point = point;
            Transits = transits;
            StartDate = startDate;
        }
    }
}