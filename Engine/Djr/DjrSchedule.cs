using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
                foreach (var entry in zipFile.Entries)
                {
                    if (entry.FullName.EndsWith("/")) continue;

                    if (String.Compare(Path.GetExtension(entry.Name), ".xml", StringComparison.InvariantCultureIgnoreCase) != 0)
                    {
                        Console.WriteLine("Unknown extension: at {0}", entry.FullName);
                        continue;
                    }

                    using (var fileStream = entry.Open())
                    {
                        LoadXmlFile(fileStream);
                    }
                }
            }

            foreach (var train in trains.Values)
            {
                foreach (var variant in train.RouteVariants)
                {
                    RoutingPoint previous = null;
                    foreach (var routePoint in variant.RoutingPoints)
                    {
                        var point = routePoint.Point;

                        if (previous != null)
                        {
                            point.NeighboringPoints.Add(previous);
                            previous.NeighboringPoints.Add(point);
                        }

                        point.PassingTrains.Add(routePoint);

                        previous = point;
                    }
                }
                trainsByNumber[train.TrainNumber] = train;
            }

            Console.WriteLine("{0} trains", trains.Count);
        }

        private void LoadXmlFile(Stream stream)
        {
            var ser = new XmlSerializer(typeof(CZPTTCISMessage));
            var message = (CZPTTCISMessage) ser.Deserialize(stream);

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
                RoutingPoint point;
                if (!points.TryGetValue(location.CountryCodeISO + ":" + location.LocationPrimaryCode, out point))
                {
                    point = new RoutingPoint
                    {
                        ID = location.CountryCodeISO + ":" + location.LocationPrimaryCode,
                        Name = location.PrimaryLocationName,
                        // TODO: CodebookEntry =, LongName
                    };
                    points.Add(point.ID, point);
                }

                if (location.OperationalTrainNumber != null)
                {
                    if (trainDef.TrainNumber == null)
                    {
                        trainDef.TrainNumber = location.OperationalTrainNumber;
                    }
                    else
                    {
                        if (!trainDef.AllTrainNumbers.Contains(location.OperationalTrainNumber))
                        {
                            Console.WriteLine("Train number mismatch: '{0}' vs '{1}'", trainDef.TrainNumber, location.OperationalTrainNumber);
                            // TODO: Remove common prefix?
                            trainDef.TrainNumber += "/" + location.OperationalTrainNumber;
                        }
                    }
                    trainDef.AllTrainNumbers.Add(location.OperationalTrainNumber);
                }
                if (location.CommercialTrafficType != null)
                {
                    var category = defTrainCategory[location.CommercialTrafficType];
                    if (trainDef.TrainCategory == TrainCategory.Unknown)
                    {
                        trainDef.TrainCategory = category;
                    }
                    else
                    {
                        if (trainDef.TrainCategory != category)
                        {
                            Console.WriteLine("Train category mismatch for {0}: {1} vs {2}", trainDef.TrainNumber, trainDef.TrainCategory, category);
                        }
                    }
                }

                var timingPerType = location.TimingAtLocation?.Timing?.ToDictionary(t => t.TimingQualifierCode);
                Timing arrivalTiming = null;
                Timing departureTiming = null;
                timingPerType?.TryGetValue("ALA", out arrivalTiming);
                timingPerType?.TryGetValue("ALD", out departureTiming);

                HashSet<TrainOperation> trainOperations;
                if (location.TrainActivity?.Count > 0)
                {
                    trainOperations = new HashSet<TrainOperation>(location.TrainActivity.Count);
                    foreach (var activity in location.TrainActivity)
                    {
                        trainOperations.Add(defTrainOperation[activity.TrainActivityType]);
                    }
                }
                else
                {
                    trainOperations = null;
                }

                routingPoints.Add(new TrainRoutePoint
                {
                    RouteVariant = routeVariant,
                    Point = point,
                    PointType = location.JourneyLocationTypeCode == null
                        ? TrainRoutePointType.Unknown
                        : defTrainRoutePointType[location.JourneyLocationTypeCode],
                    ScheduledArrival = arrivalTiming?.ToTimeSpan,
                    ScheduledDeparture = departureTiming?.ToTimeSpan,
                    SubsidiaryLocation = location.LocationSubsidiaryIdentification?.LocationSubsidiaryCode?.Code,
                    SubsidiaryLocationType = location.LocationSubsidiaryIdentification?.LocationSubsidiaryCode?.LocationSubsidiaryTypeCode == null
                        ? SubsidiaryLocationType.None
                        : defSubsidiaryLocationType[location.LocationSubsidiaryIdentification?.LocationSubsidiaryCode?.LocationSubsidiaryTypeCode],
                    SubsidiaryLocationName = location.LocationSubsidiaryIdentification?.LocationSubsidiaryName,
                    TrainOperations = trainOperations,
                });
            }
        }

        private static readonly Dictionary<string, SubsidiaryLocationType> defSubsidiaryLocationType = new Dictionary<string, SubsidiaryLocationType>
        {
            { "0", SubsidiaryLocationType.Unknown },
            { "1", SubsidiaryLocationType.StationTrack }
        };

        private static readonly Dictionary<string, TrainRoutePointType> defTrainRoutePointType = new Dictionary<string, TrainRoutePointType>
        {
            { "00", TrainRoutePointType.Unknown },
            { "01", TrainRoutePointType.Origin },
            { "02", TrainRoutePointType.Intermediate },
            { "03", TrainRoutePointType.Destination },
            { "04", TrainRoutePointType.Handover },
            { "05", TrainRoutePointType.Interchange },
            { "06", TrainRoutePointType.HandoverAndInterchange },
            { "07", TrainRoutePointType.StateBorder }
        };

        private static readonly Dictionary<string, TrainCategory> defTrainCategory = new Dictionary<string, TrainCategory>
        {
            { "50", TrainCategory.EuroCity },
            { "63", TrainCategory.Intercity },
            { "69", TrainCategory.Express },
            { "70", TrainCategory.EuroNight },
            { "84", TrainCategory.Regional },
            { "94", TrainCategory.SuperCity },
            { "122", TrainCategory.Rapid },
            { "157", TrainCategory.FastTrain },
            { "209", TrainCategory.RailJet },
            { "9000", TrainCategory.Rex },
            { "9001", TrainCategory.TrilexExpres },
            { "9002", TrainCategory.Trilex },
            { "9003", TrainCategory.LeoExpres },
            { "9004", TrainCategory.Regiojet },
            { "9005", TrainCategory.ArrivaExpress },
            { "9006", TrainCategory.NightJet }
        };

        private static readonly Dictionary<string, TrainOperation> defTrainOperation = new Dictionary<string, TrainOperation>
        {
            { "0001", TrainOperation.StopRequested },
            { "0026", TrainOperation.Customs },
            { "0027", TrainOperation.Other },
            { "0028", TrainOperation.EmbarkOnly },
            { "0029", TrainOperation.DisembarkOnly },
            { "0030", TrainOperation.RequestStop },
            { "0031", TrainOperation.DepartOnArrival },
            { "0032", TrainOperation.DepartAfterDisembark },
            { "0033", TrainOperation.NoWaitForConnections },
            { "0035", TrainOperation.Preheating },
            { "0040", TrainOperation.Passthrough },
            { "0043", TrainOperation.ConnectedTrains },
            { "0044", TrainOperation.TrainConnection },
            { "CZ01", TrainOperation.StopsDuringOpeningHours },
            { "CZ02", TrainOperation.ShortStop },
            { "CZ03", TrainOperation.HandicappedEmbark },
            { "CZ04", TrainOperation.HandicappedDisembark },
            { "CZ05", TrainOperation.WaitForDelayedTrains },
            { "0002", TrainOperation.OperationalStopOnly },
            { "CZ13", TrainOperation.NonpublicStop }
        };
    }

    public class Train
    {
        public string PathCompany { get; set; }
        public string PathCore { get; set; }
        public string PathTimetableYear { get; set; }
        public string TrainCompany { get; set; }
        public string TrainCore { get; set; }
        public string TrainTimetableYear { get; set; }

        public string TrainNumber { get; set; }
        public HashSet<string> AllTrainNumbers { get; } = new HashSet<string>(1);
        public string TrainName { get; set; }
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

        public string ID => String.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", Train.TrainCompany, Train.TrainCore, TrainVariant);
    }

    public class TrainCalendar
    {
        public BitArray CalendarBitmap { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }

        public DateTime BaseDate => ValidFrom;
    }

    public class TrainRoutePoint : IComparable<TrainRoutePoint>
    {
        public RoutingPoint Point { get; set; }
        public RouteVariant RouteVariant { get; set; }

        public Train Train => RouteVariant.Train;
        public TrainCalendar Calendar => RouteVariant.Calendar;

        public TrainRoutePointType PointType { get; set; }

        public string SubsidiaryLocation { get; set; }
        public string SubsidiaryLocationName { get; set; }
        public SubsidiaryLocationType SubsidiaryLocationType { get; set; }

        public TimeSpan? ScheduledArrival { get; set; }
        public TimeSpan? ScheduledDeparture { get; set; }

        public TimeSpan AnyScheduledTime => ScheduledArrival ?? ScheduledDeparture.GetValueOrDefault();

        public HashSet<TrainOperation> TrainOperations { get; set; }

        public int CompareTo(TrainRoutePoint other)
        {
            if (other == null) return +1;
            if (ReferenceEquals(this, other)) return 0;

            var timeResult = AnyScheduledTime.CompareTo(other.AnyScheduledTime);
            if (timeResult != 0) return timeResult;

            // TODO: Train.ID
            return String.CompareOrdinal(RouteVariant.ID, other.RouteVariant.ID);
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

        public string LongName => Name; // CodebookEntry?.LongName;
        public PointType Type => PointType.Unknown; // CodebookEntry.Type
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
        Unknown,
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
        None,
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
        Unknown,
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