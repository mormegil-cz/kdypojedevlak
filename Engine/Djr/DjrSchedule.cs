using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;
using KdyPojedeVlak.Engine.Djr.DjrXmlModel;

namespace KdyPojedeVlak.Engine.Djr
{
    public class DjrSchedule
    {
        private readonly Dictionary<string, RoutingPoint> points = new Dictionary<string, RoutingPoint>();
        private readonly Dictionary<string, Train> trains = new Dictionary<string, Train>();
        private readonly Dictionary<string, Train> trainsByNumber = new Dictionary<string, Train>();

        public Dictionary<string, RoutingPoint> Points => points;
        public Dictionary<string, Train> Trains => trainsByNumber;

        private string path;

        public DjrSchedule(string path)
        {
            this.path = path;
        }

        public void Load()
        {
            if (points.Count > 0) throw new InvalidOperationException("Already loaded");

            using (var zipFile = ZipFile.OpenRead(path))
            {
                foreach (var entry in zipFile.Entries.Take(100))
                {
                    if (entry.FullName.EndsWith("/")) continue;

                    if (String.Compare(Path.GetExtension(entry.Name), ".xml", StringComparison.InvariantCultureIgnoreCase) != 0)
                    {
                        Console.WriteLine("Unknown extension: at {0}", entry.FullName);
                        continue;
                    }

                    using (var fileStream = entry.Open())
                    {
                        Console.Write("Loading {0}... ", entry.Name);
                        LoadXmlFile(fileStream);
                        Console.WriteLine();
                    }
                }
            }

            Console.WriteLine("{0} trains", trains.Count);
        }

        private void LoadXmlFile(Stream stream)
        {
            var ser = new XmlSerializer(typeof(CZPTTCISMessage));
            var message = (CZPTTCISMessage) ser.Deserialize(stream);
            Console.Write("{0} points ", message.CZPTTInformation.CZPTTLocation.Count);

            var identifiersPerType = message.Identifiers.PlannedTransportIdentifiers.ToDictionary(pti => pti.ObjectType);
            var trainId = identifiersPerType["TR"];
            var pathId = identifiersPerType["PA"];

            Train trainDef;
            if (!trains.TryGetValue(trainId.Company + "/" + trainId.Core, out trainDef))
            {
                trainDef = new Train
                {
                    PathCompany = pathId.Company,
                    PathCore = pathId.Core,
                    PathTimetableYear = pathId.TimetableYear,
                    TrainCompany = trainId.Company,
                    TrainCore = trainId.Core,
                    TrainTimetableYear = trainId.TimetableYear
                };
                trains.Add(trainId.Company + "/" + trainId.Core, trainDef);
            }

            if (trainDef.PathCompany != pathId.Company) Console.WriteLine("PathCompany mismatch: '{0}' vs '{1}'", trainDef.PathCompany, pathId.Company);
            if (trainDef.PathCore != pathId.Core) Console.WriteLine("PathCore mismatch: '{0}' vs '{1}'", trainDef.PathCore, pathId.Core);
            if (trainDef.PathTimetableYear != pathId.TimetableYear) Console.WriteLine("PathTimetableYear mismatch: '{0}' vs '{1}'", trainDef.PathTimetableYear, pathId.TimetableYear);
            if (trainDef.TrainCompany != trainId.Company) Console.WriteLine("TrainCompany mismatch: '{0}' vs '{1}'", trainDef.TrainCompany, trainId.Company);
            if (trainDef.TrainCore != trainId.Core) Console.WriteLine("TrainCore mismatch: '{0}' vs '{1}'", trainDef.TrainCore, trainId.Core);
            if (trainDef.TrainTimetableYear != trainId.TimetableYear) Console.WriteLine("TrainTimetableYear mismatch: '{0}' vs '{1}'", trainDef.TrainTimetableYear, trainId.TimetableYear);

            foreach (var variant in trainDef.RouteVariants)
            {
                if (variant.PathVariant == pathId.Variant || variant.TrainVariant == trainId.Variant)
                {
                    Console.WriteLine("Duplicate variant in {0}: '{1}', '{2}'", trainId.Core, trainId.Variant, pathId.Variant);
                    break;
                }
            }
            var routingPoints = new List<TrainRoutePoint>();
            var routeVariant = new RouteVariant
            {
                Train = trainDef,
                Calendar = new TrainCalendar
                {
                    CalendarBitmap = new BitArray(message.CZPTTInformation.PlannedCalendar.BitmapDays.Select(c => c == '1').ToArray()),
                    ValidFrom = message.CZPTTInformation.PlannedCalendar.ValidityPeriod.StartDateTime,
                    ValidTo = message.CZPTTInformation.PlannedCalendar.ValidityPeriod.EndDateTime
                },
                PathVariant = pathId.Variant,
                TrainVariant = trainId.Variant,
                RoutingPoints = routingPoints
            };
            trainDef.RouteVariants.Add(routeVariant);
            foreach (var location in message.CZPTTInformation.CZPTTLocation)
            {
                routingPoints.Add(new TrainRoutePoint
                {
                    RouteVariant = routeVariant
                });
            }
        }
    }

    public class Train
    {
        public string PathCompany { get; set; }
        public string PathCore { get; set; }
        public string PathTimetableYear { get; set; }
        public string TrainCompany { get; set; }
        public string TrainCore { get; set; }
        public string TrainTimetableYear { get; set; }

        public string Name { get; set; }
        public TrainType TrainType { get; set; }
        public TrafficType TrafficType { get; set; }
        public TrainCategory TrainCategory { get; set; }

        // ?check: move to TrainRoutePoint?
        public string OperationalTrainNumber { get; set; }

        public List<RouteVariant> RouteVariants { get; } = new List<RouteVariant>();
    }

    public class RouteVariant
    {
        public Train Train { get; set; }

        // ?check: Split PathVariants to another level?
        public string PathVariant { get; set; }
        public string TrainVariant { get; set; }

        public TrainCalendar Calendar { get; set; }
        public List<TrainRoutePoint> RoutingPoints { get; set; }
    }

    public class TrainCalendar
    {
        public BitArray CalendarBitmap { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
    }

    public class TrainRoutePoint
    {
        public RoutingPoint Point { get; set; }
        public RouteVariant RouteVariant { get; set; }

        public TrainRoutePointType PointType { get; set; }

        public string SubsidiaryLocation { get; set; }
        public string SubsidiaryLocationName { get; set; }
        public SubsidiaryLocationType SubsidiaryLocationType { get; set; }

        public TimeSpan? ScheduledArrival { get; set; }
        public TimeSpan? ScheduledDeparture { get; set; }

        public HashSet<TrainOperation> TrainOperations { get; set; }
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

    public enum TrainType
    {
        Unknown,
        PersonalNonPublic,
        PersonalPublic,
        Cargo
    }

    public enum TrafficType
    {
        Unknown,
        Os,
        Ex,
        R,
        Sp,
    }

    public enum TrainCategory
    {
        EuroCity,
        Intercity,
        Express,
        EuroNight,
        Regional,
        SuperCity,
        Rapid,
        FastTrain,
        RailJet,
        Rex,
        TrilexExpres,
        Trilex,
        LeoExpres,
        Regiojet,
        ArrivaExpress,
        NightJet
    }

    public enum TrainRoutePointType
    {
        Unknown,
        Origin,
        Intermediate,
        Destination,
        Handover,
        Interchange,
        HandoverAndInterchange,
        StateBorder
    }

    public enum SubsidiaryLocationType
    {
        Unknown,
        StationTrack
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

    public enum TrainOperation
    {
        StopRequested,
        Customs,
        Other,
        EmbarkOnly,
        DisembarkOnly,
        RequestStop,
        DepartOnArrival,
        DepartAfterDisembark,
        NoWaitForConnections,
        Preheating,
        Passthrough,
        ConnectedTrains,
        TrainConnection,
        StopsDuringOpeningHours,
        ShortStop,
        HandicappedEmbark,
        HandicappedDisembark,
        WaitForDelayedTrains,
        OperationalStopOnly,
        NonpublicStop
    }
}