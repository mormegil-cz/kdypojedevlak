using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CoreFtp.Infrastructure.Extensions;

namespace KdyPojedeVlak.Engine
{
    public class Train
    {
        public string ID { get; set; }
        public string TrainNumber { get; set; }
        public string TrainType { get; set; }
        public string TrainName { get; set; }
        public List<TrainRoutePoint> Route { get; }

        public Train()
        {
            Route = new List<TrainRoutePoint>();
        }
    }

    public class TrainCalendar
    {
        public string ID { get; set; }
        public string Description { get; set; }
        public bool[] Bitmap { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
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

        public int CompareTo(TrainRoutePoint other)
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
        public SortedSet<TrainRoutePoint> PassingTrains { get; }
        public HashSet<RoutingPoint> NeighboringPoints { get; }

        public RoutingPoint()
        {
            PassingTrains = new SortedSet<TrainRoutePoint>();
            NeighboringPoints = new HashSet<RoutingPoint>();
        }

        public string LongName
        {
            get { return CodebookEntry.LongName; }
        }

        public PointType Type
        {
            get { return CodebookEntry.Type; }
        }
    }

    public class PointCodebookEntry
    {
        public string ID { get; set; }
        public string LongName { get; set; }
        public string ShortName { get; set; }
        public PointType Type { get; set; }
    }

    public enum PointType
    {
        Unknown,
        Stop,
        Station,
        InnerBoundary,
        StateBoundary,
        Crossing,
        Siding,
        Point
    }

    public class KangoSchedule
    {
        private static readonly string[] trainTypesByQuality = {"SC", "IC", "EN", "EC", "Ex", "R", "Sp", "Os"};
        private readonly string path;
        private readonly Dictionary<string, RoutingPoint> points = new Dictionary<string, RoutingPoint>();
        private readonly Dictionary<string, Train> trains = new Dictionary<string, Train>();
        private readonly Dictionary<string, Train> trainsByNumber = new Dictionary<string, Train>();

        public Dictionary<string, RoutingPoint> Points => points;
        public Dictionary<string, Train> Trains => trainsByNumber;

        public DateTime BitmapBaseDate { get; private set; }

        static KangoSchedule()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public KangoSchedule(string path)
        {
            this.path = path;
        }

        public void Load()
        {
            if (points.Count > 0) throw new InvalidOperationException("Already loaded");

            var pointCodebook = new Dictionary<string, PointCodebookEntry>();
            LoadCsvData(Path.Combine(path, @"..\"), @"SR70-2017-12-10.csv")
                .Select(r => new {ID = "0054-" + r[0].PadLeft(6, '0') + "-00", Row = r})
                .IntoDictionary(pointCodebook, r => r.ID, r => new PointCodebookEntry
                {
                    ID = r.Row[0],
                    LongName = r.Row[1],
                    ShortName = r.Row[2],
                    Type = ParsePointType(r.Row[5])
                });

            LoadKangoData(path, "DB")
                .Select(b => new {ID = BuildPointId(b, 0), Name = b[3]})
                .IntoDictionary(points, b => b.ID, b => new RoutingPoint
                {
                    ID = b.ID,
                    Name = b.Name,
                    CodebookEntry = pointCodebook.GetValueOrDefault(b.ID) ?? new PointCodebookEntry
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
                .Select((row, idx) => new {Row = row, Index = idx / 2})
                .GroupBy(row => row.Index)
                .ToDictionary(g => g.Last().Row[0], g => new TrainCalendar
                {
                    ID = g.Last().Row[0],
                    Description = g.Last().Row[2] + g.Last().Row[3],
                    ValidFrom = GetDateFromRow(g.First().Row, 0),
                    ValidTo = GetDateFromRow(g.First().Row, 3),
                    Bitmap = g.Last().Row[1].Select(c => c == '1').ToArray()
                });

            BitmapBaseDate = calendars.Values.Min(c => c.ValidFrom);

            calendars.Add("0", new TrainCalendar {ID = "0", Description = "jede pp", Bitmap = new bool[553]});
            calendars.Add("1",
                new TrainCalendar
                {
                    ID = "1",
                    Description = "",
                    Bitmap = Enumerable.Range(1, 553).Select(_ => true).ToArray()
                });

            LoadKangoData(path, "HLV")
                .IntoDictionary(trains, t => t[0], t => new Train
                {
                    ID = t[0],
                    TrainNumber = t[0].Split('/')[0],
                    TrainType = trainTypes[t[0]],
                    TrainName = t[1]
                });
            trains.Values
                .IntoDictionary(trainsByNumber, t => t.TrainNumber, t => t);

            Train currTrain = null;
            TrainCalendar currCalendar = null;
            TimeSpan? lastTime = null;
            RoutingPoint lastRoutingPoint = null;
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
                if (!String.IsNullOrEmpty(row[37])) currCalendar = calendars[row[37]];

                lastTime = arrival ?? departure ?? lastTime;
                if (lastTime == null) throw new NotSupportedException(String.Format("{0}: No time", currTrain));

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

        private static PointType ParsePointType(string typeStr)
        {
            PointType type;
            return !pointTypePerName.TryGetValue(typeStr, out type) ? PointType.Unknown : type;
        }

        public IEnumerable<TrainRoutePoint> GetPassesThrough(string pointID)
        {
            RoutingPoint point;
            if (!points.TryGetValue(pointID, out point)) return null;
            return point.PassingTrains;
        }

        private static IEnumerable<string[]> LoadKangoData(string path, string extension)
        {
            return LoadKangoData(Directory.EnumerateFiles(path, "*." + extension).Single());
        }

        private static IEnumerable<string[]> LoadKangoData(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(stream, Encoding.GetEncoding(1250)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        yield return line.Split('|');
                    }
                }
            }
        }

        private IEnumerable<string[]> LoadCsvData(string path, string fileName)
        {
            using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(stream, Encoding.GetEncoding(1250)))
                {
                    string line;
                    bool firstLine = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        /*
                        TODO: Real CSV processing
                        if (line.Contains('"'))
                        {
                        }
                        else
                        */
                        {
                            if (firstLine)
                            {
                                firstLine = false;
                                continue;
                            }
                            yield return line.Split(';');
                        }
                    }
                }
            }
        }

        private static string BuildPointId(string[] row, int start)
        {
            return String.Concat(row[start + 0], "-", row[start + 1], "-", row[start + 2]);
        }

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
                    if (isRequired) throw new FormatException(String.Format("No data at {0}, {1}", col1, col2));
                    return null;
                }
                if (!Int32.TryParse(row[col2], out result))
                {
                    throw new FormatException(String.Format("Bad data at {0}: '{1}'", col2, row[col2]));
                }
            }
            else
            {
                if (!Int32.TryParse(row[col1], out result))
                {
                    throw new FormatException(String.Format("Bad data at {0}: '{1}'", col1, row[col1]));
                }
            }
            return result;
        }

        private static int? GetNumberFromRow(string[] row, int col, bool isRequired)
        {
            int result;
            if (String.IsNullOrEmpty(row[col]))
            {
                if (isRequired) throw new FormatException(String.Format("No data at {0}", col));
                return null;
            }
            else
            {
                if (!Int32.TryParse(row[col], out result))
                {
                    throw new FormatException(String.Format("Bad data at {0}: '{1}'", col, row[col]));
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

        private static readonly Dictionary<string, PointType> pointTypePerName = new Dictionary<string, PointType>(StringComparer.OrdinalIgnoreCase)
        {
            {"Automatické hradlo", PointType.Point},
            {"Automatické hradlo a zastávka", PointType.Stop},
            {"Automatické hradlo, nákladiště a zastávka", PointType.Stop},
            {"Dopr.body na cizím území (blíže neurčené)", PointType.Point},
            {"Dopravna D3", PointType.Point},
            {"Dopravna radiobloku", PointType.Point},
            {"Hláska", PointType.Point},
            {"Hláska a zastávka", PointType.Stop},
            {"Hláska, nákladiště a zastávka", PointType.Stop},
            {"Hradlo", PointType.Point},
            {"Hradlo a zastávka", PointType.Stop},
            {"Hranice infrastruktur", PointType.InnerBoundary},
            {"Hranice oblastí", PointType.InnerBoundary},
            {"Hranice OPŘ totožná s hranicí VÚSC", PointType.InnerBoundary},
            {"Hranice TUDU (začátek nebo konec TUDU)", PointType.InnerBoundary},
            {"Hranice třídy sklonu", PointType.InnerBoundary},
            {"Jiné dopravní body", PointType.Point},
            {"Kolejová křižovatka", PointType.Crossing},
            {"Kolejová skupina stanice nebo jiného DVM", PointType.Station},
            {"Nákladiště", PointType.Point},
            {"Nákladiště a zastávka", PointType.Stop},
            {"Obvod DVM nebo staniční kolejová skupina se zastávkou", PointType.Stop},
            {"Odbočení ve stanici nebo v jiném DVM", PointType.Crossing},
            {"Odbočení vlečky", PointType.Crossing},
            {"Odbočka (dopravna s kolejovým rozvětvením)", PointType.Crossing},
            {"Odbočka (dopravna) a zastávka", PointType.Stop},
            {"Odbočka (dopravna), nákladiště a zastávka", PointType.Stop},
            {"Odstup nezavěšeného postrku (na širé trati)", PointType.Point},
            {"Samostatné kolejiště vlečky", PointType.Point},
            {"Samostatné tarif.místo v rámci stanice nebo jiného DVM", PointType.Point},
            {"Skok ve staničení", PointType.Point},
            {"Stanice (z přepravního hlediska blíže neurčená)", PointType.Station},
            {"Státní hranice", PointType.StateBoundary},
            {"Trasovací bod TUDU, SENA", PointType.Point},
            {"Výhybna", PointType.Siding},
            {"Zastávka", PointType.Stop},
            {"Zastávka lanové dráhy", PointType.Stop},
            {"Zastávka náhradní autobusové dopravy", PointType.Stop},
            {"Zastávka v obvodu stanice", PointType.Stop},
            {"Závorářské stanoviště", PointType.Point}
        };
    }
}
