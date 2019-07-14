﻿using System.Collections.Generic;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Engine.Djr;
using RoutingPoint = KdyPojedeVlak.Engine.DbStorage.RoutingPoint;

namespace KdyPojedeVlak.Models
{
    public class TrainPlan
    {
        public TrainTimetable Train { get; }
        public List<RoutingPoint> Points { get; }
        public List<List<Passage>> VariantRoutingPoints { get; }
        public List<bool> MajorPointFlags { get; }

        public TrainPlan(TrainTimetable train, List<RoutingPoint> points, List<List<Passage>> variantRoutingPoints, List<bool> majorPointFlags)
        {
            Train = train;
            Points = points;
            VariantRoutingPoints = variantRoutingPoints;
            MajorPointFlags = majorPointFlags;
        }
    }
}