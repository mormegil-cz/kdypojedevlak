using KdyPojedeVlak.Web.Engine.Djr;

namespace KdyPojedeVlak.Web.Models;

public class BasicTrainInfo(int timetableYear, string number, string name, string? firstPointName, string? lastPointName, TrainCategory trainCategory, TrafficType trafficType)
{
    public int TimetableYear { get; } = timetableYear;
    public string Number { get; } = number;
    public string Name { get; } = name;
    public string? FirstPointName { get; } = firstPointName;
    public string? LastPointName { get; } = lastPointName;
    public TrainCategory TrainCategory { get; } = trainCategory;
    public TrafficType TrafficType { get; } = trafficType;
}