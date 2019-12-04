#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;
using KdyPojedeVlak.Engine.Algorithms;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Engine.Djr.DjrXmlModel;
using KdyPojedeVlak.Engine.SR70;
using Microsoft.EntityFrameworkCore;

namespace KdyPojedeVlak.Engine.Djr
{
    public static class DjrSchedule
    {
        public static void ImportNewFiles(DbModelContext dbModelContext, Dictionary<string, long> availableDataFiles)
        {
            foreach (var file in availableDataFiles)
            {
                ImportCompressedDataFile(file.Key, file.Value, dbModelContext);
            }
        }

        public static void RenameAllCalendars(DbModelContext dbModelContext)
        {
            foreach (var calendar in dbModelContext.CalendarDefinitions)
            {
                var newName = CalendarNamer.DetectName(calendar.Bitmap, calendar.StartDate, calendar.EndDate);
                if (newName != calendar.Description)
                {
                    DebugLog.LogDebugMsg("Changed calendar name from '{0}' to '{1}'", calendar.Description, newName);
                    calendar.Description = newName;
                }
            }
            dbModelContext.SaveChanges();
        }

        private static bool IsGzip(string filename)
        {
            using var file = File.OpenRead(filename);
            return file.Length > 2 && file.ReadByte() == 0x1F && file.ReadByte() == 0x8B;
        }

        private static void ImportCompressedDataFile(string filename, long fileSize, DbModelContext dbModelContext)
        {
            DebugLog.LogDebugMsg("Importing {0}", filename);
            if (IsGzip(filename))
            {
                ImportDataFile(filename, fileSize, dbModelContext, () => ReadGzipFile(filename));
            }
            else
            {
                using var zipFile = ZipFile.OpenRead(filename);
                foreach (var entry in zipFile.Entries)
                {
                    if (entry.FullName.EndsWith("/")) continue;

                    if (String.Compare(Path.GetExtension(entry.Name), ".xml", StringComparison.InvariantCultureIgnoreCase) != 0)
                    {
                        DebugLog.LogProblem("Unknown extension: at {0}", entry.FullName);
                        continue;
                    }

                    ImportDataFile($"{filename}#{entry.FullName}", entry.Length, dbModelContext, () => ReadZipEntry(entry));
                }
            }
        }

        private static void ImportDataFile(string fileName, long fileSize, DbModelContext dbModelContext, Func<Stream> fileReader)
        {
            var alreadyImportedFile = dbModelContext.ImportedFiles.SingleOrDefault(f => f.FileName == fileName);
            if (alreadyImportedFile != null && alreadyImportedFile.FileSize == fileSize)
            {
                DebugLog.LogDebugMsg("File {0} already imported", fileName);
                return;
            }

            if (alreadyImportedFile != null)
            {
                DebugLog.LogProblem("Imported file size mismatch: {0} imported as {1} B, now has {2} B", fileName, fileSize, alreadyImportedFile.FileSize);
                return;
            }

            using (var transaction = dbModelContext.Database.BeginTransaction())
            {
                CZPTTCISMessage message;
                using (var stream = fileReader())
                {
                    message = LoadXmlFile(stream);
                }

                var creationDate = message.CZPTTCreation;
                var importedFile = new ImportedFile
                {
                    FileName = fileName,
                    FileSize = fileSize,
                    ImportTime = DateTime.Now,
                    CreationDate = creationDate
                };
                dbModelContext.ImportedFiles.Add(importedFile);

                var trainNumber = ImportToDatabase(message, importedFile, dbModelContext);

                dbModelContext.SaveChanges();

                transaction.Commit();

                ScheduleVersionInfo.ReportFileImported(creationDate, trainNumber);
            }

            DebugLog.LogDebugMsg("File {0} imported successfully", fileName);
        }

        private static Stream ReadGzipFile(string filename) => new GZipStream(File.OpenRead(filename), CompressionMode.Decompress);

        private static Stream ReadZipEntry(ZipArchiveEntry entry) => entry.Open();

        private static CZPTTCISMessage LoadXmlFile(Stream stream)
        {
            var ser = new XmlSerializer(typeof(CZPTTCISMessage));
            return (CZPTTCISMessage) ser.Deserialize(stream);
        }

        private static string ImportToDatabase(CZPTTCISMessage message, ImportedFile importedFile, DbModelContext dbModelContext)
        {
            var identifiersPerType = message.Identifiers.PlannedTransportIdentifiers.ToDictionary(pti => pti.ObjectType);
            var trainId = identifiersPerType["TR"];
            var pathId = identifiersPerType["PA"];
            var trainIdentifier = trainId.Company + "/" + trainId.Core + "/" + trainId.Variant;
            var pathIdentifier = pathId.Company + "/" + pathId.Core + "/" + pathId.Variant;

            if (trainId.TimetableYear != pathId.TimetableYear)
            {
                DebugLog.LogProblem("TimetableYear mismatch at {0}: {1} vs {2}", trainId.Core, trainId.TimetableYear, pathId.TimetableYear);
            }

            var plannedCalendar = message.CZPTTInformation.PlannedCalendar;
            var calendarMinDate = plannedCalendar.ValidityPeriod.StartDateTime;
            var calendarMaxDate = plannedCalendar.ValidityPeriod.EndDateTime ?? calendarMinDate.AddDays(message.CZPTTInformation.PlannedCalendar.BitmapDays.Length - 1);

            var timetableYear = trainId.TimetableYear;
            var dbTimetableYear = dbModelContext.TimetableYears.SingleOrDefault(y => y.Year == timetableYear);
            if (dbTimetableYear == null)
            {
                dbTimetableYear = new TimetableYear
                {
                    Year = timetableYear,
                    MinDate = calendarMinDate,
                    MaxDate = calendarMaxDate
                };
                dbModelContext.TimetableYears.Add(dbTimetableYear);
                DebugLog.LogDebugMsg("Created year {0}", timetableYear);
            }
            else
            {
                if (dbTimetableYear.MinDate > calendarMinDate) dbTimetableYear.MinDate = calendarMinDate;
                if (dbTimetableYear.MaxDate < calendarMaxDate) dbTimetableYear.MaxDate = calendarMaxDate;
            }

            // TODO: Clarify TrainNumbers
            var operationalTrainNumbers = message.CZPTTInformation.CZPTTLocation.Select(loc => loc.OperationalTrainNumber).Where(n => n != null).ToHashSet();
            if (operationalTrainNumbers.Count > 1)
            {
                DebugLog.LogProblem("Train {0} contains {1} operational numbers", trainIdentifier, operationalTrainNumbers.Count);
            }
            var operationalTrainNumber = operationalTrainNumbers.FirstOrDefault();

            var networkSpecificParameters = message.NetworkSpecificParameter.ToDictionary(param => param.Name, param => param.Value);
            networkSpecificParameters.TryGetValue(NetworkSpecificParameterGlobal.CZTrainName.ToString(), out var trainName);
//            if (networkSpecificParameters.Count - (trainName != null ? 1 : 0) > 0)
//            {
//                Console.WriteLine(trainId.Company + "/" + trainId.Core);
//            }

            var train = dbModelContext.Trains.SingleOrDefault(t => t.Number == operationalTrainNumber);
            if (train == null)
            {
                train = new Train
                {
                    Number = operationalTrainNumber
                };
                dbModelContext.Trains.Add(train);
                DebugLog.LogDebugMsg("Created train {0}", operationalTrainNumber);
            }

            var calendarBitmap = plannedCalendar.BitmapDays.Select(c => c == '1').ToArray();
            var trainCalendar = new CalendarDefinition
            {
                // TODO: trainCalendar.BaseDate??
                Description = CalendarNamer.DetectName(calendarBitmap, calendarMinDate, calendarMaxDate),
                StartDate = calendarMinDate,
                EndDate = calendarMaxDate,
                TimetableYear = dbTimetableYear,
                Bitmap = calendarBitmap
            };
            var dbCalendar = dbModelContext.CalendarDefinitions.SingleOrDefault(c => c.Guid == trainCalendar.Guid);
            if (dbCalendar == null)
            {
                dbCalendar = trainCalendar;
                dbModelContext.CalendarDefinitions.Add(dbCalendar);
                DebugLog.LogDebugMsg("Created calendar {0}", trainCalendar.Guid);
            }

            var trainCategories = message.CZPTTInformation.CZPTTLocation.Where(loc => loc.CommercialTrafficType != null).Select(loc => defTrainCategory[loc.CommercialTrafficType]).ToHashSet();
            if (trainCategories.Count > 1)
            {
                DebugLog.LogProblem("Train {0} contains {1} categories", trainIdentifier, trainCategories.Count);
            }

            var trafficTypes = message.CZPTTInformation.CZPTTLocation.Where(loc => loc.TrafficType != null).Select(loc => defTrafficType[loc.TrafficType]).ToHashSet();
            if (trafficTypes.Count > 1)
            {
                DebugLog.LogProblem("Train {0} contains {1} traffic types", trainIdentifier, trafficTypes.Count);
            }

            var trainTimetable = dbModelContext.TrainTimetables.Include(tt => tt.Variants).SingleOrDefault(tt => tt.Train == train && tt.TimetableYear == dbTimetableYear);
            if (trainTimetable == null)
            {
                trainTimetable = new TrainTimetable
                {
                    Train = train,
                    TimetableYear = dbTimetableYear,
                    Name = trainName,
                    Data = new Dictionary<string, string>
                    {
                        { TrainTimetable.AttribTrafficType, trafficTypes.FirstOrDefault().ToString() },
                        { TrainTimetable.AttribTrainCategory, trainCategories.FirstOrDefault().ToString() },
                        // { TrainTimetable.AttribTrainType, train.TrainType.ToString() },
                    },
                    Variants = new List<TrainTimetableVariant>()
                };
                dbModelContext.TrainTimetables.Add(trainTimetable);
            }

            if (trainTimetable.Variants.Any(ttv => ttv.PathVariantId == pathIdentifier && ttv.TrainVariantId == trainIdentifier))
            {
                DebugLog.LogProblem("Duplicate variant: '{0}', '{1}'", pathIdentifier, trainIdentifier);
            }

            var trainTimetableVariant = new TrainTimetableVariant
            {
                Timetable = trainTimetable,
                Calendar = dbCalendar,
                PathVariantId = pathIdentifier,
                TrainVariantId = trainIdentifier,
                ImportedFrom = importedFile,
                Data = new Dictionary<string, string>
                {
                    // TODO: Train timetable variant data
                },
                Points = new List<Passage>()
            };
            dbModelContext.Add(trainTimetableVariant);

            var locationIndex = 0;
            RoutingPoint? prevPoint = null;
            foreach (var location in message.CZPTTInformation.CZPTTLocation)
            {
                var locationID = location.CountryCodeISO + ":" + location.LocationPrimaryCode;
                var dbPoint = dbModelContext.RoutingPoints.SingleOrDefault(rp => rp.Code == locationID);
                if (dbPoint == null)
                {
                    var codebookEntry = Program.PointCodebook.Find(locationID) ?? new PointCodebookEntry
                    {
                        ID = locationID,
                        LongName = location.PrimaryLocationName ?? $"#{location.LocationPrimaryCode}",
                        ShortName = location.PrimaryLocationName,
                        Type = location.CountryCodeISO == "CZ" ? PointType.Unknown : PointType.Point
                    };

                    dbPoint = new RoutingPoint
                    {
                        Code = locationID,
                        Name = codebookEntry.LongName,
                        Latitude = codebookEntry.Latitude,
                        Longitude = codebookEntry.Longitude,
                        Data = new Dictionary<string, string>
                        {
                            // TODO: Routing point data
                        }
                    };
                    dbModelContext.RoutingPoints.Add(dbPoint);
                    dbModelContext.SaveChanges();
                    DebugLog.LogDebugMsg("Created point '{0}'", codebookEntry.LongName);
                }

                var timingPerType = location.TimingAtLocation?.Timing?.ToDictionary(t => t.TimingQualifierCode);
                Timing? arrivalTiming = null;
                Timing? departureTiming = null;
                timingPerType?.TryGetValue("ALA", out arrivalTiming);
                timingPerType?.TryGetValue("ALD", out departureTiming);

                ISet<TrainOperation> trainOperations;
                if (location.TrainActivity?.Count > 0)
                {
                    trainOperations = new SortedSet<TrainOperation>();
                    foreach (var activity in location.TrainActivity)
                    {
                        trainOperations.Add(defTrainOperation[activity.TrainActivityType]);
                    }
                }
                else
                {
                    trainOperations = Sets<TrainOperation>.Empty;
                }

                if (arrivalTiming != null && arrivalTiming.Equals(departureTiming))
                {
                    if (!trainOperations.Contains(TrainOperation.ShortStop) &&
                        !trainOperations.Contains(TrainOperation.RequestStop))
                    {
                        arrivalTiming = null;
                    }
                }

                if (prevPoint != null)
                {
                    // TODO: Cache for NeighboringPointTuples?
                    /*
                    var tuple = ValueTuple.Create(prevPoint.Code, dbPoint.Code);
                    if (!pointTuples.Contains(tuple))
                    */

                    if (!dbModelContext.NeighboringPointTuples.Any(npt => npt.PointA == prevPoint && npt.PointB == dbPoint))
                    {
                        dbModelContext.NeighboringPointTuples.Add(new NeighboringPoints
                        {
                            PointA = prevPoint,
                            PointB = dbPoint
                        });
                        dbModelContext.SaveChanges();
                        DebugLog.LogDebugMsg("Point '{0}' follows '{1}'", dbPoint.Name, prevPoint.Name);
                    }
                }

                prevPoint = dbPoint;

                var passage = new Passage
                {
                    Year = dbTimetableYear,
                    TrainTimetableVariant = trainTimetableVariant,
                    Order = locationIndex++,
                    Point = dbPoint,
                    ArrivalDay = arrivalTiming?.Offset ?? 0,
                    ArrivalTime = arrivalTiming?.AsTimeSpan(),
                    DepartureDay = departureTiming?.Offset ?? 0,
                    DepartureTime = departureTiming?.AsTimeSpan(),
                    DwellTime = location.TimingAtLocation?.DwellTime,
                    Data = new Dictionary<string, string?>
                    {
                        // TODO: JourneyLocationTypeCode
                        { Passage.AttribTrainOperations, String.Join(';', trainOperations) },
                        { Passage.AttribSubsidiaryLocation, location.LocationSubsidiaryIdentification?.LocationSubsidiaryCode?.Code },
                        { Passage.AttribSubsidiaryLocationName, location.LocationSubsidiaryIdentification?.LocationSubsidiaryName },
                        {
                            Passage.AttribSubsidiaryLocationType,
                            (location.LocationSubsidiaryIdentification?.LocationSubsidiaryCode
                                 ?.LocationSubsidiaryTypeCode == null
                                ? SubsidiaryLocationType.None
                                : defSubsidiaryLocationType[
                                    location.LocationSubsidiaryIdentification?.LocationSubsidiaryCode
                                        ?.LocationSubsidiaryTypeCode ?? "0"]).ToString()
                        },
                    }
                };
                trainTimetableVariant.Points.Add(passage);
                dbModelContext.Add(passage);
            }

            dbModelContext.SaveChanges();

            return operationalTrainNumber;
        }

        private static readonly Dictionary<string, SubsidiaryLocationType> defSubsidiaryLocationType =
            new Dictionary<string, SubsidiaryLocationType>
            {
                { "0", SubsidiaryLocationType.Unknown },
                { "1", SubsidiaryLocationType.StationTrack }
            };

        private static readonly Dictionary<string, TrainRoutePointType> defTrainRoutePointType =
            new Dictionary<string, TrainRoutePointType>
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

        private static readonly Dictionary<string, TrafficType> defTrafficType =
            new Dictionary<string, TrafficType>
            {
                { "11", TrafficType.Os },
                { "C1", TrafficType.Ex },
                { "C2", TrafficType.R },
                { "C3", TrafficType.Sp },
                { "C4", TrafficType.Sv },
                { "C5", TrafficType.Nex },
                { "C6", TrafficType.Pn },
                { "C7", TrafficType.Mn },
                { "C8", TrafficType.Lv },
                { "C9", TrafficType.Vleč },
                { "CA", TrafficType.Služ },
                { "CB", TrafficType.Pom },
            };

        private static readonly Dictionary<string, TrainCategory> defTrainCategory =
            new Dictionary<string, TrainCategory>
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
                { "9006", TrainCategory.NightJet },
                { "9007", TrainCategory.LeoExpresTenders },
            };

        private static readonly Dictionary<string, TrainOperation> defTrainOperation =
            new Dictionary<string, TrainOperation>
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
                { "CZ01", TrainOperation.StopsAfterOpening },
                { "CZ02", TrainOperation.ShortStop },
                { "CZ03", TrainOperation.HandicappedEmbark },
                { "CZ04", TrainOperation.HandicappedDisembark },
                { "CZ05", TrainOperation.WaitForDelayedTrains },
                { "0002", TrainOperation.OperationalStopOnly },
                { "CZ13", TrainOperation.NonpublicStop }
            };
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
        Sv,
        Nex,
        Pn,
        Mn,
        Lv,
        Vleč,
        Služ,
        Pom
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
        NightJet,
        LeoExpresTenders,
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
        StopsAfterOpening,
        ShortStop,
        HandicappedEmbark,
        HandicappedDisembark,
        WaitForDelayedTrains,
        OperationalStopOnly,
        NonpublicStop
    }

    public enum NetworkSpecificParameterGlobal
    {
        Unknown,
        CZReroute,
        CZOriginalCalendarStartDate,
        CZOriginalCalendarEndDate,
        CZOriginalCalendarBitmaps,
        CZCentralPTTNote,
        CZNonCentralPTTNote,
        CZCalendarPTTNote,
        CZTrainName
    }
}