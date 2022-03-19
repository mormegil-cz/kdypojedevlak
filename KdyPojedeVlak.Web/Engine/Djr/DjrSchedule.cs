#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;
using KdyPojedeVlak.Web.Engine.Algorithms;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Djr.DjrXmlModel;
using KdyPojedeVlak.Web.Engine.SR70;
using Microsoft.EntityFrameworkCore;

namespace KdyPojedeVlak.Web.Engine.Djr
{
    public static class DjrSchedule
    {
        public static void ImportNewFiles(DbModelContext dbModelContext, Dictionary<string, long> availableDataFiles)
        {
            // %%NOCOMMIT: remove temporary patch
            foreach (var file in availableDataFiles.Where(df => df.Key.Contains(@"\2021\")))
            {
                ImportCompressedDataFile(file.Key, file.Value, dbModelContext);
            }
            DebugLog.LogDebugMsg("Import done");
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

        public static void RecomputeYearLimits(DbModelContext dbModelContext)
        {
            foreach (var year in dbModelContext.TimetableYears)
            {
                var minDate = dbModelContext.CalendarDefinitions.Where(c => c.TimetableYear == year).Min(c => (DateTime?)c.StartDate);
                var maxDate = dbModelContext.CalendarDefinitions.Where(c => c.TimetableYear == year).Max(c => (DateTime?)c.EndDate);

                if (minDate != null && year.MinDate > minDate)
                {
                    DebugLog.LogProblem("Changed MinDate for {0} from {1} to {2}", year.Year, year.MinDate, minDate);
                    year.MinDate = minDate.GetValueOrDefault();
                }
                if (maxDate != null && year.MaxDate < maxDate)
                {
                    DebugLog.LogProblem("Changed MaxDate for {0} from {1} to {2}", year.Year, year.MaxDate, maxDate);
                    year.MaxDate = maxDate.GetValueOrDefault();
                }

                if (minDate != null && year.MinDate != minDate)
                {
                    DebugLog.LogProblem("MinDate mismatch for {0}: {1} vs {2}", year.Year, year.MinDate, minDate);
                }
                if (maxDate != null && year.MaxDate != maxDate)
                {
                    DebugLog.LogProblem("MaxDate mismatch for {0}: {1} vs {2}", year.Year, year.MaxDate, maxDate);
                }
            }
            dbModelContext.SaveChanges();
        }

        public static void ReloadPointCoordinates(DbModelContext dbModelContext)
        {
            var pointCodebook = Program.PointCodebook;
            foreach (var point in dbModelContext.RoutingPoints)
            {
                var pointCodebookEntry = pointCodebook.Find(point.Code);
                if (pointCodebookEntry == null)
                {
                    DebugLog.LogDebugMsg("No codebook entry for {0}", point.Code);
                    continue;
                }

                if (pointCodebookEntry.Latitude == null || pointCodebookEntry.Longitude == null) continue;
                var lat = pointCodebookEntry.Latitude.GetValueOrDefault();
                var lon = pointCodebookEntry.Longitude.GetValueOrDefault();

                if (point.Latitude == null || point.Longitude == null)
                {
                    DebugLog.LogDebugMsg("Filling missing coordinates for {0}", point.Code);
                    point.Latitude = lat;
                    point.Longitude = lon;
                }
                else
                {
                    var currLat = point.Latitude.GetValueOrDefault();
                    var currLon = point.Longitude.GetValueOrDefault();
                    var dist = Math.Abs(lat - currLat) + Math.Abs(lon - currLon);
                    if (dist > 0.005)
                    {
                        DebugLog.LogProblem(String.Format(CultureInfo.InvariantCulture, "Fixing wrong geographical position for point #{0} ({6}): {1}, {2} versus {3}, {4}: {5}", point.Code, lat, lon, currLat, currLon, dist * 40000.0f / 360.0f, pointCodebookEntry.WikidataItem));
                    }

                    point.Latitude = lat;
                    point.Longitude = lon;
                }
            }
            dbModelContext.SaveChanges();
        }

        public static int FillMissingPointNames(DbModelContext dbModelContext)
        {
            int count = 0;
            var pointCodebook = Program.PointCodebook;
            foreach (var point in dbModelContext.RoutingPoints.Where(rp => rp.Name.StartsWith("#")))
            {
                if (point.Name != "#" + point.ShortCzechIdentifier)
                {
                    DebugLog.LogProblem(String.Format(CultureInfo.InvariantCulture, "Suspicious name for point {0}: {1}", point.Code, point.Name));
                    continue;
                }

                var codebookEntry = pointCodebook.Find(point.Code);
                if (codebookEntry == null)
                {
                    DebugLog.LogProblem(String.Format(CultureInfo.InvariantCulture, "Unknown name for point {0}", point.Code));
                }
                else
                {
                    DebugLog.LogProblem(String.Format(CultureInfo.InvariantCulture, "Fixing name for point {0}: {1} → {2}", point.Code, point.Name, codebookEntry.LongName));
                    point.Name = codebookEntry.LongName;
                    ++count;
                }
            }
            dbModelContext.SaveChanges();
            return count;
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
            return (CZPTTCISMessage)ser.Deserialize(stream);
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
            if (operationalTrainNumbers.Count != 1)
            {
                DebugLog.LogProblem("Train {0} contains {1} operational numbers", trainIdentifier, operationalTrainNumbers.Count);
            }
            var operationalTrainNumber = operationalTrainNumbers.FirstOrDefault();

            var networkSpecificParameters = ReadNetworkSpecificParameters(message.NetworkSpecificParameter);
            networkSpecificParameters.TryGetValue(NetworkSpecificParameterGlobal.CZTrainName.ToString(), out var trainNames);
            var trainName = trainNames == null ? null : String.Join('/', trainNames);

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
            var dbCalendar = FindOrCreateCalendar(dbModelContext, new CalendarDefinition
            {
                // TODO: trainCalendar.BaseDate??
                Description = CalendarNamer.DetectName(calendarBitmap, calendarMinDate, calendarMaxDate),
                StartDate = calendarMinDate,
                EndDate = calendarMaxDate,
                TimetableYear = dbTimetableYear,
                Bitmap = calendarBitmap
            });

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
                Points = new List<Passage>()
            };
            dbModelContext.Add(trainTimetableVariant);

            var locationIndex = 0;
            RoutingPoint? prevPoint = null;
            var passages = new Dictionary<string, List<Passage>>(message.CZPTTInformation.CZPTTLocation.Count);
            foreach (var locationDirect in LinqExtensions.ConcatExisting<LocationBasicInfo>(message.CZPTTHeader?.CZForeignOriginLocation, message.CZPTTInformation.CZPTTLocation, message.CZPTTHeader?.CZForeignDestinationLocation))
            {
                var locationData = locationDirect.Location ?? locationDirect;
                if (String.IsNullOrWhiteSpace(locationData.CountryCodeISO) || String.IsNullOrWhiteSpace(locationData.LocationPrimaryCode)) throw new FormatException("Missing location identifiers");
                var locationRawID = locationData.CountryCodeISO + locationData.LocationPrimaryCode;
                var locationID = locationData.CountryCodeISO + ":" + locationData.LocationPrimaryCode;
                var dbPoint = dbModelContext.RoutingPoints.SingleOrDefault(rp => rp.Code == locationID);
                if (dbPoint == null)
                {
                    var codebookEntry = Program.PointCodebook.Find(locationID) ?? new PointCodebookEntry
                    {
                        ID = locationID,
                        LongName = locationData.PrimaryLocationName ?? $"#{locationData.LocationPrimaryCode}",
                        ShortName = locationData.PrimaryLocationName,
                        Type = locationData.CountryCodeISO == "CZ" ? PointType.Unknown : PointType.Point
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

                var locationFull = locationDirect as CZPTTLocation;
                var timingPerType = locationFull?.TimingAtLocation?.Timing?.ToDictionary(t => t.TimingQualifierCode);
                Timing? arrivalTiming = null;
                Timing? departureTiming = null;
                timingPerType?.TryGetValue("ALA", out arrivalTiming);
                timingPerType?.TryGetValue("ALD", out departureTiming);

                ISet<TrainOperation> trainOperations;
                if ((locationFull?.TrainActivity?.Count ?? 0) > 0)
                {
                    trainOperations = new SortedSet<TrainOperation>();
                    foreach (var activity in locationFull!.TrainActivity)
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
                    DwellTime = locationFull?.TimingAtLocation?.DwellTime,
                    Data = new Dictionary<string, string?>
                    {
                        // TODO: JourneyLocationTypeCode
                        { Passage.AttribTrainOperations, String.Join(';', trainOperations) },
                        { Passage.AttribSubsidiaryLocation, locationFull?.LocationSubsidiaryIdentification?.LocationSubsidiaryCode?.Code },
                        { Passage.AttribSubsidiaryLocationName, locationFull?.LocationSubsidiaryIdentification?.LocationSubsidiaryName },
                        {
                            Passage.AttribSubsidiaryLocationType,
                            (locationFull?.LocationSubsidiaryIdentification?.LocationSubsidiaryCode
                                ?.LocationSubsidiaryTypeCode == null
                                ? SubsidiaryLocationType.None
                                : defSubsidiaryLocationType[
                                    locationFull?.LocationSubsidiaryIdentification?.LocationSubsidiaryCode
                                        ?.LocationSubsidiaryTypeCode ?? "0"]).ToString()
                        },
                    },
                };
                trainTimetableVariant.Points.Add(passage);
                dbModelContext.Add(passage);

                if (passages.Count == 0) passages["_FIRST"] = new List<Passage>(1) { passage };

                if (!passages.TryGetValue(locationRawID, out var passageListPerID)) passageListPerID = new List<Passage>(2);
                passageListPerID.Add(passage);
                passages[locationRawID] = passageListPerID;

                var networkSpecificParametersForPassage = ReadNetworkSpecificParameters(locationFull?.NetworkSpecificParameter);
                if (networkSpecificParametersForPassage != null)
                {
                    foreach (var param in networkSpecificParametersForPassage)
                    {
                        dbModelContext.Add(
                            new NetworkSpecificParameterForPassage
                            {
                                Passage = passage,
                                Type = Enum.Parse<NetworkSpecificParameterPassage>(param.Key),
                                Value = String.Join(';', param.Value)
                            });
                    }
                }
            }

            if (prevPoint != null)
            {
                passages["_LAST"] = new List<Passage>(1) { trainTimetableVariant.Points.Last() };
            }

            dbModelContext.SaveChanges();

            // 0. List<string> → List<string[]>
            var calendarDefinitionPieces = networkSpecificParameters.TryGetValue(NetworkSpecificParameterGlobal.CZCalendarPTTNote.ToString(), out var calendarPttNoteDefinitionLines) ? calendarPttNoteDefinitionLines.Select(line => line.Split('|')).ToList() : null;

            // 1. List<string> → Dictionary<string, string>
            var calendarDefinitionStrings = calendarDefinitionPieces == null ? null : MergeNetworkSpecificCalendarDefinitions(calendarDefinitionPieces);

            // 2. Dictionary<string, string> → Dictionary<string, CalendarDefinition>
            var pttNoteCalendars = calendarDefinitionStrings == null ? null : ParseNetworkSpecificCalendarDefinitions(calendarDefinitionStrings, dbTimetableYear, dbModelContext);

            if (networkSpecificParameters.TryGetValue(NetworkSpecificParameterGlobal.CZCentralPTTNote.ToString(), out var centralPttNoteDefinitions))
            {
                dbModelContext.AddRange(centralPttNoteDefinitions.Select(def => ParseCentralPttNoteDefinition(def, passages, pttNoteCalendars, trainTimetableVariant)));
            }
            if (networkSpecificParameters.TryGetValue(NetworkSpecificParameterGlobal.CZNonCentralPTTNote.ToString(), out var nonCentralPttNoteDefinitions))
            {
                dbModelContext.AddRange(nonCentralPttNoteDefinitions.Select(def => ParseNonCentralPttNoteDefinition(def, passages, pttNoteCalendars, trainTimetableVariant)));
            }
            dbModelContext.SaveChanges();

            return operationalTrainNumber;
        }

        private static Dictionary<string, List<string>>? ReadNetworkSpecificParameters(List<NetworkSpecificParameter>? parameters)
            => parameters?.GroupBy(param => param.Name)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Value).ToList());

        private static CalendarDefinition FindOrCreateCalendar(DbModelContext dbModelContext, CalendarDefinition trainCalendar)
        {
            var dbCalendar = dbModelContext.CalendarDefinitions.SingleOrDefault(c => c.Guid == trainCalendar.Guid);
            if (dbCalendar != null) return dbCalendar;

            dbModelContext.CalendarDefinitions.Add(trainCalendar);
            dbModelContext.SaveChanges();
            DebugLog.LogDebugMsg("Created calendar {0}", trainCalendar.Guid);
            return trainCalendar;
        }

        private static IDictionary<string, string[]> MergeNetworkSpecificCalendarDefinitions(List<string[]> lines)
        {
            var result = lines.ToDictionaryLax(pieces => pieces[0], pieces => pieces);
            var merged = new HashSet<string>();
            foreach (var entry in result)
            {
                var mergeId = entry.Value.Length < 5 ? null : entry.Value[4];
                if (!String.IsNullOrEmpty(mergeId))
                {
                    entry.Value[3] += result[mergeId][3];
                    merged.Add(mergeId);
                }
            }
            result.RemoveAll(merged);
            return result;
        }

        private static IDictionary<string, CalendarDefinition> ParseNetworkSpecificCalendarDefinitions(IDictionary<string, string[]> calendarDefinitionStrings, TimetableYear dbTimetableYear, DbModelContext dbModelContext)
        {
            var pttNoteCalendars = new Dictionary<string, CalendarDefinition>(calendarDefinitionStrings.Count);
            foreach (var def in calendarDefinitionStrings)
            {
                var pieces = def.Value;
                var id = pieces[0];
                if (id != def.Key) throw new FormatException("Calendar ID mismatch");
                var bitmapStr = pieces[3];
                var bitmap = bitmapStr.Select(c => c == '1').ToArray();
                var startDate = DateTime.ParseExact(pieces[1], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                var endDate = pieces[2].Length == 0 ? startDate.AddDays(bitmap.Length - 1) : DateTime.ParseExact(pieces[2], "yyyyMMdd", CultureInfo.InvariantCulture);
                if ((endDate - startDate).TotalDays >= bitmap.Length)
                {
                    throw new FormatException("Too short bitmap");
                }
                var calendar = FindOrCreateCalendar(dbModelContext, new CalendarDefinition
                {
                    TimetableYear = dbTimetableYear,
                    StartDate = startDate,
                    EndDate = endDate,
                    Bitmap = bitmap,
                    Description = CalendarNamer.DetectName(bitmap, startDate, endDate)
                });
                pttNoteCalendars.Add(id, calendar);
            }
            return pttNoteCalendars;
        }

        private static CentralPttNoteForVariant ParseCentralPttNoteDefinition(string definition, Dictionary<string, List<Passage>> passages, IDictionary<string, CalendarDefinition>? calendarDefinitions, TrainTimetableVariant trainTimetableVariant)
        {
            var pieces = definition.Split('|');

            var fromCode = pieces[1];
            var fromOccurrence = String.IsNullOrEmpty(pieces[2]) ? 0 : Int32.Parse(pieces[2]);
            var toCode = pieces[3];
            var toOccurrence = String.IsNullOrEmpty(pieces[4]) ? 0 : Int32.Parse(pieces[4]);

            if (!passages.TryGetValue(fromCode, out var fromList)) fromList = passages["_FIRST"];
            if (!passages.TryGetValue(toCode, out var toList)) toList = passages["_LAST"];

            var from = fromList[fromOccurrence];
            var to = toList[toOccurrence];

            return new CentralPttNoteForVariant
            {
                TrainTimetableVariant = trainTimetableVariant,
                Type = defCentralPttNote[pieces[0]],
                From = from,
                To = to,
                OnArrival = pieces[5] == "1",
                Calendar = calendarDefinitions == null ? trainTimetableVariant.Calendar : calendarDefinitions[pieces[6]]
            };
        }

        private static NonCentralPttNoteForVariant ParseNonCentralPttNoteDefinition(string definition, Dictionary<string, List<Passage>> passages, IDictionary<string, CalendarDefinition>? calendarDefinitions, TrainTimetableVariant trainTimetableVariant)
        {
            var pieces = definition.Split('|');

            var fromCode = pieces[0];
            var fromOccurrence = String.IsNullOrEmpty(pieces[1]) ? 0 : Int32.Parse(pieces[1]);
            var toCode = pieces[2];
            var toOccurrence = String.IsNullOrEmpty(pieces[3]) ? 0 : Int32.Parse(pieces[3]);

            if (!passages.TryGetValue(fromCode, out var fromList)) fromList = passages["_FIRST"];
            if (!passages.TryGetValue(toCode, out var toList)) toList = passages["_LAST"];

            var from = fromList[fromOccurrence];
            var to = toList[toOccurrence];

            return new NonCentralPttNoteForVariant
            {
                TrainTimetableVariant = trainTimetableVariant,
                Text = pieces[4],
                From = from,
                To = to,
                ShowInHeader = defShowInHeader[pieces[5]],
                ShowInFooter = defShowInFooter[pieces[6]],
                IsTariff = pieces[7] == "1",
                OnArrival = pieces[8] == "1",
                Calendar = calendarDefinitions == null ? trainTimetableVariant.Calendar : calendarDefinitions[pieces[9]],
            };
        }

        private static readonly Dictionary<string, SubsidiaryLocationType> defSubsidiaryLocationType =
            new()
            {
                { "0", SubsidiaryLocationType.Unknown },
                { "1", SubsidiaryLocationType.StationTrack }
            };

        private static readonly Dictionary<string, TrainRoutePointType> defTrainRoutePointType =
            new()
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
            new()
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
            new()
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
            new()
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

        private static readonly Dictionary<string, CentralPttNote> defCentralPttNote =
            new()
            {
                { "10", CentralPttNote.Class12 },
                { "11", CentralPttNote.Class1 },
                { "12", CentralPttNote.Class2 },
                { "13", CentralPttNote.SleepingCar },
                { "14", CentralPttNote.CouchetteCar },
                { "15", CentralPttNote.DirectCar },
                { "16", CentralPttNote.Cars },
                { "17", CentralPttNote.Disabled },
                { "18", CentralPttNote.Restaurant },
                { "19", CentralPttNote.Reservation },
                { "20", CentralPttNote.ObligatoryReservation },
                { "21", CentralPttNote.Baggage },
                { "22", CentralPttNote.Bicycle },
                { "23", CentralPttNote.Transfer },
                { "24", CentralPttNote.Refreshments },
                { "25", CentralPttNote.Cafe },
                { "26", CentralPttNote.BaggageReservation },
                { "27", CentralPttNote.BaggageObligatoryReservation },
                { "28", CentralPttNote.BicycleReservation },
                { "29", CentralPttNote.BicycleObligatoryReservation },
                { "30", CentralPttNote.PowerSocket },
                { "32", CentralPttNote.ReplacementBus },
                { "33", CentralPttNote.Children },
                { "34", CentralPttNote.DisabledPlatform },
                { "35", CentralPttNote.SelfService },
                { "36", CentralPttNote.NoBicycles },
                { "37", CentralPttNote.HistoricTrain },
                { "38", CentralPttNote.WomenSectionCD },
                { "39", CentralPttNote.SilentSectionCD },
                { "40", CentralPttNote.WifiCD },
                { "41", CentralPttNote.PortalCD },
                { "42", CentralPttNote.CinemaCD },
                { "44", CentralPttNote.IntegratedTransportSystem },
                { "45", CentralPttNote.DirectedBoarding }
            };

        private static readonly Dictionary<String, HeaderDisplay> defShowInHeader =
            new()
            {
                { "1", HeaderDisplay.None },
                { "2", HeaderDisplay.Icon },
                { "3", HeaderDisplay.IconIfWholeRoute },
            };

        private static readonly Dictionary<String, FooterDisplay> defShowInFooter =
            new()
            {
                { "0", FooterDisplay.None },
                { "1", FooterDisplay.Everything },
                { "2", FooterDisplay.EverythingExceptSection },
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

    public enum CentralPttNote
    {
        Unknown,
        Class12,
        Class1,
        Class2,
        SleepingCar,
        CouchetteCar,
        DirectCar,
        Cars,
        Disabled,
        Restaurant,
        Reservation,
        ObligatoryReservation,
        Baggage,
        Bicycle,
        Transfer,
        Refreshments,
        Cafe,
        BaggageReservation,
        BaggageObligatoryReservation,
        BicycleReservation,
        BicycleObligatoryReservation,
        PowerSocket,
        ReplacementBus,
        Children,
        DisabledPlatform,
        SelfService,
        NoBicycles,
        HistoricTrain,
        WomenSectionCD,
        SilentSectionCD,
        WifiCD,
        PortalCD,
        CinemaCD,
        Unknown43,
        IntegratedTransportSystem,
        DirectedBoarding,
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

    public enum NetworkSpecificParameterPassage
    {
        Unknown,
        CZPassengerServiceNumber,
        CZPublicService,
        CZAlternativeTransport,
    }

    public enum HeaderDisplay
    {
        None,
        Icon,
        IconIfWholeRoute
    }

    public enum FooterDisplay
    {
        None,
        Everything,
        EverythingExceptSection
    }
}