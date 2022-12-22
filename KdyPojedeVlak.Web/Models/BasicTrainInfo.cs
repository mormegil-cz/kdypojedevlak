using System.Collections.Generic;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Djr;

namespace KdyPojedeVlak.Web.Models;

public class BasicTrainInfo
{
    private readonly Dictionary<string, string> data;

    public BasicTrainInfo(int timetableYear, string number, string name, string dataJson, string firstPointName, string lastPointName)
    {
        this.TimetableYear = timetableYear;
        this.Number = number;
        this.Name = name;
        this.FirstPointName = firstPointName;
        this.LastPointName = lastPointName;
        this.data = DbModelUtils.LoadDataJson(dataJson);
    }

    public TrainCategory TrainCategory => DbModelUtils.GetAttributeEnum(data, TrainTimetable.AttribTrainCategory, TrainCategory.Unknown);

    public TrafficType TrafficType => DbModelUtils.GetAttributeEnum(data, TrainTimetable.AttribTrafficType, TrafficType.Unknown);

    public int TimetableYear { get; init; }
    public string Number { get; init; }
    public string Name { get; init; }
    public string FirstPointName { get; init; }
    public string LastPointName { get; init; }
}