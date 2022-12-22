#nullable enable

using System;
using System.Collections.Generic;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Djr;
using KdyPojedeVlak.Web.Helpers;

namespace KdyPojedeVlak.Web.Models
{
    public class NearestTransits
    {
        public RoutingPoint Point { get; }
        public DateTime StartDate { get; }
        public IEnumerable<Transit> Transits { get; }

        public int CurrentTimetableYear { get; }

        public HashSet<RoutingPoint> NeighboringPoints { get; }
        public List<RoutingPoint>? NearestPoints { get; }

        public NearestTransits(RoutingPoint point, DateTime startDate, int currentTimetableYear, IEnumerable<Transit> transits, HashSet<RoutingPoint> neighboringPoints, List<RoutingPoint>? nearestPoints)
        {
            Point = point;
            StartDate = startDate;
            CurrentTimetableYear = currentTimetableYear;
            Transits = transits;
            NeighboringPoints = neighboringPoints;
            NearestPoints = nearestPoints == null || nearestPoints.Count == 0 ? null : nearestPoints;
        }

        public record Transit(int TimetableYear, CalendarDefinition Calendar, TimeSpan? ArrivalTime, TimeSpan? DepartureTime, string DataJson, string? TrainNumber, string? TrainName, string? SubsidiaryLocationDescription, string? PreviousPointName, string? NextPointName)
        {
            public TimeSpan? AnyScheduledTime => ArrivalTime ?? DepartureTime;

            public TimeSpan? AnyScheduledTimeOfDay => AnyScheduledTime?.GetTimeOfDay();

            public TrainCategory TrainCategory => DbModelUtils.GetAttributeEnum(DbModelUtils.LoadDataJson(DataJson), TrainTimetable.AttribTrainCategory, TrainCategory.Unknown);
        }
    }
}