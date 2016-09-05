using System.Collections.Generic;
using KdyPojedeVlak.Engine;

namespace KdyPojedeVlak.Models
{
    public class NearestTransits
    {
        public RoutingPoint Point { get; }
        public IEnumerable<TrainRoutePoint> Transits { get; }

        public NearestTransits(RoutingPoint point, IEnumerable<TrainRoutePoint> transits)
        {
            this.Point = point;
            this.Transits = transits;
        }
    }
}
