using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdyPojedeVlak.Engine
{
    public class Train
    {
        public string ID { get; set; }
        public string TrainNumber { get; set; }
        public string TrainType { get; set; }
        public string TrainName { get; set; }
        public List<TrainRoutePoint> Route { get; private set; }

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
    }

    public class TrainRoutePoint : IComparable<TrainRoutePoint>
    {
        public RoutingPoint Point { get; set; }
        public Train Train { get; set; }
        public TimeSpan ScheduledTime { get; set; }
        public TrainCalendar Calendar { get; set; }

        public int CompareTo(TrainRoutePoint other)
        {
            if (other == null) return +1;
            var timeResult = ScheduledTime.CompareTo(other.ScheduledTime);
            if (timeResult != 0) return timeResult;
            return Train.ID.CompareTo(other.Train.ID);
        }
    }

    public class RoutingPoint
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public SortedSet<TrainRoutePoint> PassingTrains { get; private set; }

        public RoutingPoint()
        {
            PassingTrains = new SortedSet<TrainRoutePoint>();
        }
    }

    public class KangoSchedule
    {
        private readonly string path;
        private readonly Dictionary<string, RoutingPoint> points = new Dictionary<string, RoutingPoint>();
        private readonly Dictionary<string, Train> trains = new Dictionary<string, Train>();
        private readonly Dictionary<string, Train> trainsByNumber = new Dictionary<string, Train>();

        public Dictionary<string, RoutingPoint> Points { get { return points; } }
        public Dictionary<string, Train> Trains { get { return trainsByNumber; } }

        public KangoSchedule(string path)
        {
            this.path = path;
        }

        public void Load()
        {
            if (points.Count > 0) throw new InvalidOperationException("Already loaded");

            LoadKangoData(path, "DB")
                .Select(b => new { ID = BuildPointId(b, 0), Name = b[3] })
                .IntoDictionary(points, b => b.ID, b => new RoutingPoint
                {
                    ID = b.ID,
                    Name = b.Name
                });

            // TODO: Use per-position train types
            var trainTypes = new Dictionary<string, string>();
            foreach (var tt in LoadKangoData(path, "DVL"))
            {
                string trainType = tt[9];
                string currType;
                if (trainTypes.TryGetValue(tt[0], out currType)) trainType = currType + "/" + trainType;
                trainTypes[tt[0]] = trainType;
            }

            var calendars = LoadKangoData(path, "KVL")
                .Select((row, idx) => new { Row = row, Index = idx / 2 })
                .GroupBy(row => row.Index)
                .ToDictionary(g => g.Last().Row[0], g => new TrainCalendar
                {
                    ID = g.Last().Row[0],
                    Description = g.Last().Row[2] + g.Last().Row[3],
                    Bitmap = g.Last().Row[1].Select(c => c == '1').ToArray()
                });

            calendars.Add("0", new TrainCalendar { ID = "0", Description = "jede pp", Bitmap = new bool[553] });
            calendars.Add("1", new TrainCalendar { ID = "1", Description = "", Bitmap = Enumerable.Range(1, 553).Select(_ => true).ToArray() });

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

            string[] lastTimeRow = null;
            Train currTrain = null;
            TrainCalendar currCalendar = null;
            foreach (var row in LoadKangoData(path, "TRV"))
            {
                if (row[0] != currTrain?.ID)
                {
                    lastTimeRow = null;
                    currCalendar = null;
                    currTrain = trains[row[0]];
                }
                if (!String.IsNullOrEmpty(row[8]) || !String.IsNullOrEmpty(row[14])) lastTimeRow = row;
                if (!String.IsNullOrEmpty(row[37])) currCalendar = calendars[row[37]];

                if (lastTimeRow == null) throw new NotSupportedException(String.Format("{0}: No time", currTrain));

                var routingPoint = points[BuildPointId(row, 1)];
                var time = GetTimeFromRow(lastTimeRow);
                var trainRoutePoint = new TrainRoutePoint
                {
                    Train = currTrain,
                    Point = routingPoint,
                    Calendar = currCalendar,
                    ScheduledTime = time
                };
                currTrain.Route.Add(trainRoutePoint);
                routingPoint.PassingTrains.Add(trainRoutePoint);
            }

            // prune unused points
            var emptyPoints = points.Where(p => p.Value.PassingTrains.Count == 0).Select(p => p.Key).ToList();
            foreach (var ep in emptyPoints) points.Remove(ep);
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
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                    #warning TODO: Encoding.GetEncoding(1250)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        yield return line.Split('|');
                    }
                }
            }
        }

        private static string BuildPointId(string[] row, int start)
        {
            return String.Concat(row[start + 0], "-", row[start + 1], "-", row[start + 2]);
        }

        private static int? GetNumberFromRow(string[] row, int col1, int col2, bool isRequired)
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

        private static TimeSpan GetTimeFromRow(string[] row)
        {
            // TODO: Over-midnight trains
            //var dd = GetNumberFromRow(row, 7, 13, false);
            var dd = new int?(0);
            var hh = GetNumberFromRow(row, 8, 14, true);
            var mm = GetNumberFromRow(row, 9, 15, true);
            var ss = GetNumberFromRow(row, 10, 16, false);

            return new TimeSpan(dd.GetValueOrDefault(), hh.GetValueOrDefault(), mm.GetValueOrDefault(), ss.GetValueOrDefault() * 30);
        }
    }
}
