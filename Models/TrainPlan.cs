using System.Collections.Generic;
using KdyPojedeVlak.Engine.Djr;

namespace KdyPojedeVlak.Models
{
    public class TrainPlan
    {
        public Train Train { get; }
        public List<RoutingPoint> Points { get; }
        public List<List<TrainRoutePoint>> VariantRoutingPoints { get; }
        public List<bool> MajorPointFlags { get; }

        public TrainPlan(Train train, List<RoutingPoint> points, List<List<TrainRoutePoint>> variantRoutingPoints, List<bool> majorPointFlags)
        {
            Train = train;
            Points = points;
            VariantRoutingPoints = variantRoutingPoints;
            MajorPointFlags = majorPointFlags;
        }
    }
}