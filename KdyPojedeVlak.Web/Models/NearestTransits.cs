#nullable enable

using System;
using System.Collections.Generic;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Djr;
using KdyPojedeVlak.Web.Helpers;

namespace KdyPojedeVlak.Web.Models;

public class NearestTransits(RoutingPoint point, DateTime startDate, int currentTimetableYear, IEnumerable<NearestTransits.Transit> transits, HashSet<RoutingPoint> neighboringPoints, List<RoutingPoint>? nearestPoints)
{
    public RoutingPoint Point { get; } = point;
    public DateTime StartDate { get; } = startDate;
    public IEnumerable<Transit> Transits { get; } = transits;

    public int CurrentTimetableYear { get; } = currentTimetableYear;

    public HashSet<RoutingPoint> NeighboringPoints { get; } = neighboringPoints;
    public List<RoutingPoint>? NearestPoints { get; } = nearestPoints == null || nearestPoints.Count == 0 ? null : nearestPoints;

    public record Transit(int TimetableYear, CalendarDefinition Calendar, TimeSpan? ArrivalTime, TimeSpan? DepartureTime, decimal? DwellTime, TrainCategory TrainCategory, string? TrainNumber, string? TrainName, string? SubsidiaryLocationDescription, string? PreviousPointName, string? NextPointName)
    {
        public TimeSpan? AnyScheduledTime => ArrivalTime ?? DepartureTime;

        public TimeSpan? AnyScheduledTimeOfDay => AnyScheduledTime?.GetTimeOfDay();
    }
}