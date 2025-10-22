using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KdyPojedeVlak.Web.Engine.SR70;

namespace KdyPojedeVlak.Web.Engine.Kango;

public class Train
{
    public string ID { get; set; }
    public string TrainNumber { get; set; }
    public string TrainCategory { get; set; }
    public string TrainName { get; set; }
    public List<TrainRoutePoint> Route { get; } = [];
}

public class TrainCalendar
{
    public string ID { get; set; }
    public string Description { get; set; }
    public BitArray CalendarBitmap { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    // public DateTime BaseDate => Program.Schedule.BitmapBaseDate;
}

public class TrainRoutePoint : IComparable<TrainRoutePoint>
{
    public RoutingPoint Point { get; set; }

    public Train Train { get; set; }

    //public TimeSpan ScheduledTime { get; set; }
    public TrainCalendar Calendar { get; set; }

    public TimeSpan? ScheduledArrival { get; set; }
    public TimeSpan? ScheduledDeparture { get; set; }

    public TimeSpan AnyScheduledTime => ScheduledArrival ?? ScheduledDeparture.GetValueOrDefault();

    public int CompareTo(TrainRoutePoint? other)
    {
        if (other == null) return +1;
        var timeResult = AnyScheduledTime.CompareTo(other.AnyScheduledTime);
        if (timeResult != 0) return timeResult;
        return String.CompareOrdinal(Train.ID, other.Train.ID);
    }
}

public class RoutingPoint
{
    public string ID { get; set; }
    public string Name { get; set; }
    public PointCodebookEntry CodebookEntry { get; set; }
    public SortedSet<TrainRoutePoint> PassingTrains { get; } = [];
    public HashSet<RoutingPoint> NeighboringPoints { get; } = [];

    public string LongName => CodebookEntry.LongName;

    public PointType Type => CodebookEntry.Type;
}

public class KangoSchedule(string path)
{
    private static readonly string[] trainTypesByQuality = ["SC", "IC", "EN", "EC", "Ex", "R", "Sp", "Os"];
    private readonly Dictionary<string, RoutingPoint> points = new();
    private readonly Dictionary<string, Train> trains = new();
    private readonly Dictionary<string, Train> trainsByNumber = new();

    public Dictionary<string, RoutingPoint> Points => points;
    public Dictionary<string, Train> Trains => trainsByNumber;

    public DateTime BitmapBaseDate { get; private set; }

    public void Load()
    {
        if (points.Count > 0) throw new InvalidOperationException("Already loaded");

        LoadKangoData(path, "DB")
            .Select(b => new { ID = BuildPointId(b, 0), Name = b[3] })
            .IntoDictionary(points, b => b.ID, b => new RoutingPoint
            {
                ID = b.ID,
                Name = b.Name,
                CodebookEntry = Program.PointCodebook.Find(b.ID) ?? new PointCodebookEntry
                {
                    ID = b.ID,
                    LongName = b.Name,
                    ShortName = b.Name,
                    Type = PointType.Unknown
                }
            });

        // TODO: Use per-position train types
        // TODO: Use "Sv" as indicator of non-passenger train movements
        // TODO: Display only KDV train type names
        var trainTypes = new Dictionary<string, string>();
        foreach (var tt in LoadKangoData(path, "DVL").Concat(LoadKangoData(path, "KDV")))
        {
            string trainType = tt[9];
            if (!trainTypes.TryGetValue(tt[0], out var currType) || IsBetterTrainType(trainType, currType))
            {
                trainTypes[tt[0]] = trainType;
            }
        }

        var calendars = LoadKangoData(path, "KVL")
            .Select((row, idx) => new { Row = row, Index = idx / 2 })
            .GroupBy(row => row.Index)
            .ToDictionary(g => g.Last().Row[0], g => new TrainCalendar
            {
                ID = g.Last().Row[0],
                Description = g.Last().Row[2] + g.Last().Row[3],
                ValidFrom = GetDateFromRow(g.First().Row, 0),
                ValidTo = GetDateFromRow(g.First().Row, 3),
                CalendarBitmap = new BitArray(g.Last().Row[1].Select(c => c == '1').ToArray())
            });

        BitmapBaseDate = calendars.Values.Min(c => c.ValidFrom);

        calendars.Add("0", new TrainCalendar { ID = "0", Description = "jede pp", CalendarBitmap = new BitArray(553, false) });
        calendars.Add("1",
            new TrainCalendar
            {
                ID = "1",
                Description = "",
                CalendarBitmap = new BitArray(553, true)
            });

        LoadKangoData(path, "HLV")
            .IntoDictionary(trains, t => t[0], t => new Train
            {
                ID = t[0],
                TrainNumber = t[0].Split('/')[0],
                TrainCategory = trainTypes[t[0]],
                TrainName = t[1]
            });
        trains.Values
            .IntoDictionary(trainsByNumber, t => t.TrainNumber, t => t);

        Train? currTrain = null;
        TrainCalendar? currCalendar = null;
        TimeSpan? lastTime = null;
        RoutingPoint? lastRoutingPoint = null;
        foreach (var row in LoadKangoData(path, "TRV"))
        {
            if (currTrain == null || row[0] != currTrain.ID)
            {
                currCalendar = null;
                currTrain = trains[row[0]];
                lastTime = null;
                lastRoutingPoint = null;
            }
            var arrival = !String.IsNullOrEmpty(row[8]) ? GetTimeFromRow(row, 7) : new TimeSpan?();
            var departure = !String.IsNullOrEmpty(row[14]) ? GetTimeFromRow(row, 13) : new TimeSpan?();
            if (!String.IsNullOrEmpty(row[36])) currCalendar = calendars[row[36]];

            lastTime = arrival ?? departure ?? lastTime;
            if (lastTime == null) throw new NotSupportedException($"{currTrain.ID}: No time");

            if (currCalendar == null) throw new NotSupportedException($"{currTrain.ID}: No calendar");
            var routingPoint = points[BuildPointId(row, 1)];
            var trainRoutePoint = new TrainRoutePoint
            {
                Train = currTrain,
                Point = routingPoint,
                Calendar = currCalendar,
                ScheduledArrival = arrival,
                ScheduledDeparture = departure ?? lastTime
            };
            if (lastRoutingPoint != null)
            {
                if (routingPoint == lastRoutingPoint)
                {
                    lastRoutingPoint = routingPoint;
                }
                else
                {
                    lastRoutingPoint.NeighboringPoints.Add(routingPoint);
                    routingPoint.NeighboringPoints.Add(lastRoutingPoint);
                }
            }
            lastRoutingPoint = routingPoint;
            currTrain.Route.Add(trainRoutePoint);
            routingPoint.PassingTrains.Add(trainRoutePoint);
        }

        // prune unused points
        var emptyPoints = points.Where(p => p.Value.PassingTrains.Count == 0).Select(p => p.Key).ToList();
        foreach (var ep in emptyPoints) points.Remove(ep);
    }

    public IEnumerable<TrainRoutePoint>? GetPassesThrough(string pointId)
    {
        if (!points.TryGetValue(pointId, out var point)) return null;
        return point.PassingTrains;
    }

    private static IEnumerable<string[]> LoadKangoData(string path, string extension)
    {
        return LoadKangoData(Directory.EnumerateFiles(path, "*." + extension).Single());
    }

    private static IEnumerable<string[]> LoadKangoData(string filename)
    {
        using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.GetEncoding(1250));
        while (reader.ReadLine() is { } line)
        {
            yield return line.Split('|');
        }
    }

    private static string BuildPointId(string[] row, int start) => String.Concat(row[start + 0], "-", row[start + 1], "-", row[start + 2]);

    private static bool IsBetterTrainType(string type1, string type2)
    {
        var idx1 = Array.IndexOf(trainTypesByQuality, type1);
        if (idx1 < 0) return false;
        var idx2 = Array.IndexOf(trainTypesByQuality, type2);
        if (idx2 < 0) return true;

        return idx1 < idx2;
    }

    private static int? GetNumberFromRowAlt(string[] row, int col1, int col2, bool isRequired)
    {
        int result;
        if (String.IsNullOrEmpty(row[col1]))
        {
            if (String.IsNullOrEmpty(row[col2]))
            {
                if (isRequired) throw new FormatException($"No data at {col1}, {col2}");
                return null;
            }
            if (!Int32.TryParse(row[col2], out result))
            {
                throw new FormatException($"Bad data at {col2}: '{row[col2]}'");
            }
        }
        else
        {
            if (!Int32.TryParse(row[col1], out result))
            {
                throw new FormatException($"Bad data at {col1}: '{row[col1]}'");
            }
        }
        return result;
    }

    private static int? GetNumberFromRow(string[] row, int col, bool isRequired)
    {
        int result;
        if (String.IsNullOrEmpty(row[col]))
        {
            if (isRequired) throw new FormatException($"No data at {col}");
            return null;
        }
        else
        {
            if (!Int32.TryParse(row[col], out result))
            {
                throw new FormatException($"Bad data at {col}: '{row[col]}'");
            }
        }
        return result;
    }

    private static TimeSpan GetTimeFromRow(string[] row, int start)
    {
        // TODO: Over-midnight trains
        //var dd = GetNumberFromRow(row, 7, 13, false);
        var dd = new int?(0);
        var hh = GetNumberFromRow(row, start + 1, true);
        var mm = GetNumberFromRow(row, start + 2, true);
        var ss = GetNumberFromRow(row, start + 3, false);

        return new TimeSpan(dd.GetValueOrDefault(), hh.GetValueOrDefault(), mm.GetValueOrDefault(),
            ss.GetValueOrDefault() * 30);
    }

    private static DateTime GetDateFromRow(string[] row, int start)
    {
        var dd = GetNumberFromRow(row, start, true);
        var mm = GetNumberFromRow(row, start + 1, true);
        var yy = GetNumberFromRow(row, start + 2, true);

        return new DateTime(yy.GetValueOrDefault(), mm.GetValueOrDefault(), dd.GetValueOrDefault());
    }
}