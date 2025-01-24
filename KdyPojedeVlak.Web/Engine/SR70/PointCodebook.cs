using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KdTree;
using KdTree.Math;

namespace KdyPojedeVlak.Web.Engine.SR70;

public class PointCodebook(string path)
{
    private static readonly Regex regexGeoCoordinate = new Regex(@"\s*^[NE]\s*(?<deg>[0-9]+)\s*°\s*(?<min>[0-9]*)\s*'\s*(?<sec>[0-9]*\s*(,\s*([0-9]+)?)?)\s*""\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

    private Dictionary<string, PointCodebookEntry> codebook;
    private KdTree<float, string> tree;

    static PointCodebook()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public void Load()
    {
        if (codebook != null) throw new InvalidOperationException("Already loaded");

        codebook = new Dictionary<string, PointCodebookEntry>();

        CodebookHelpers.LoadCsvData(path, @"SR70-2024-12-15.csv", ';', Encoding.GetEncoding(1250))
            .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r))
            .IntoDictionary(codebook, r => r.ID, r => new PointCodebookEntry
            {
                ID = r.Row[0],
                LongName = r.Row[1],
                ShortName = r.Row[3],
                Type = ParsePointType(r.Row[10]),
                Longitude = ParseGeoCoordinate(r.Row[28]),
                Latitude = ParseGeoCoordinate(r.Row[29]),
            });

        // add historical data for missing points

        /*
        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2024-10-15.csv", ';', Encoding.GetEncoding(1250))
                     .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[1],
                ShortName = point.Row[3],
                Type = ParsePointType(point.Row[10]),
                Longitude = ParseGeoCoordinate(point.Row[28]),
                Latitude = ParseGeoCoordinate(point.Row[29]),
            });

            DebugLog.LogDebugMsg("Additional point in 2024-10 codebook: {0}", point.ID);
        }

        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2024-04-15.csv", ';', Encoding.GetEncoding(1250))
                     .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[1],
                ShortName = point.Row[3],
                Type = ParsePointType(point.Row[10]),
                Longitude = ParseGeoCoordinate(point.Row[28]),
                Latitude = ParseGeoCoordinate(point.Row[29]),
            });

            DebugLog.LogDebugMsg("Additional point in 2024-04 codebook: {0}", point.ID);
        }

        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2023-12-10.csv", ';', Encoding.GetEncoding(1250))
                     .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[1],
                ShortName = point.Row[3],
                Type = ParsePointType(point.Row[10]),
                Longitude = ParseGeoCoordinate(point.Row[28]),
                Latitude = ParseGeoCoordinate(point.Row[29]),
            });

            DebugLog.LogDebugMsg("Additional point in 2023-12 codebook: {0}", point.ID);
        }

        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2023-08-15.csv", ';', Encoding.GetEncoding(1250))
                     .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[1],
                ShortName = point.Row[3],
                Type = ParsePointType(point.Row[10]),
                Longitude = ParseGeoCoordinate(point.Row[28]),
                Latitude = ParseGeoCoordinate(point.Row[29]),
            });

            DebugLog.LogDebugMsg("Additional point in 2023-08 codebook: {0}", point.ID);
        }

        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2022-10-15.csv", ';', Encoding.GetEncoding(1250))
                     .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[1],
                ShortName = point.Row[3],
                Type = ParsePointType(point.Row[10]),
                Longitude = ParseGeoCoordinate(point.Row[28]),
                Latitude = ParseGeoCoordinate(point.Row[29]),
            });

            DebugLog.LogDebugMsg("Additional point in 2022-10 codebook: {0}", point.ID);
        }

        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2022-09-01.csv", ';', Encoding.GetEncoding(1250))
                     .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[1],
                ShortName = point.Row[3],
                Type = ParsePointType(point.Row[10]),
                Longitude = ParseGeoCoordinate(point.Row[28]),
                Latitude = ParseGeoCoordinate(point.Row[29]),
            });

            DebugLog.LogDebugMsg("Additional point in 2022-04 codebook: {0}", point.ID);
        }

        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2022-04-15.csv", ';', Encoding.GetEncoding(1250))
            .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[1],
                ShortName = point.Row[3],
                Type = ParsePointType(point.Row[10]),
                Longitude = ParseGeoCoordinate(point.Row[28]),
                Latitude = ParseGeoCoordinate(point.Row[29]),
            });

            DebugLog.LogDebugMsg("Additional point in 2022-04 codebook: {0}", point.ID);
        }

        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2021-12-31.csv", ';', Encoding.GetEncoding(1250))
            .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[2],
                ShortName = point.Row[4],
                Type = ParsePointType(point.Row[11]),
                Longitude = ParseGeoCoordinate(point.Row[29]),
                Latitude = ParseGeoCoordinate(point.Row[30]),
            });

            DebugLog.LogDebugMsg("Additional point in 2020-12 codebook: {0}", point.ID);
        }

        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2020-09-14.csv", ';', Encoding.GetEncoding(1250))
            .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[1],
                ShortName = point.Row[3],
                Type = ParsePointType(point.Row[10]),
                Longitude = ParseGeoCoordinate(point.Row[28]),
                Latitude = ParseGeoCoordinate(point.Row[29]),
            });

            DebugLog.LogDebugMsg("Additional point in 2020-09 codebook: {0}", point.ID);
        }
        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2020-06-14.csv", ';', Encoding.GetEncoding(1250))
            .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[2],
                ShortName = point.Row[4],
                Type = ParsePointType(point.Row[11]),
                Longitude = ParseGeoCoordinate(point.Row[29]),
                Latitude = ParseGeoCoordinate(point.Row[30]),
            });

            DebugLog.LogDebugMsg("Additional point in 2020-06 codebook: {0}", point.ID);
        }
        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2019-12-15.csv", ';', Encoding.GetEncoding(1250))
            .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.Row[0],
                LongName = point.Row[1],
                ShortName = point.Row[3],
                Type = ParsePointType(point.Row[9]),
                Longitude = ParseGeoCoordinate(point.Row[21]),
                Latitude = ParseGeoCoordinate(point.Row[22]),
            });

            DebugLog.LogDebugMsg("Additional point in 2019 codebook: {0}", point.ID);
        }
        */

        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2017-12-10.csv", ';', Encoding.GetEncoding(1250))
                     .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.ID,
                LongName = point.Row[1],
                ShortName = point.Row[2],
                Type = ParsePointType(point.Row[5]),
            });

            DebugLog.LogDebugMsg("Additional point in 2017 codebook: {0}", point.ID);
        }
        foreach (var point in CodebookHelpers.LoadCsvData(path, @"SR70-2013-12-15.csv", ';', Encoding.GetEncoding(1250))
                     .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r)))
        {
            if (codebook.ContainsKey(point.ID)) continue;

            codebook.Add(point.ID, new PointCodebookEntry
            {
                ID = point.ID,
                LongName = point.Row[1],
                ShortName = point.Row[2],
                Type = ParsePointType(point.Row[7]),
            });

            DebugLog.LogDebugMsg("Additional point in 2013 codebook: {0}", point.ID);
        }

        var problematicPoints = new HashSet<String>();
        foreach (var row in CodebookHelpers.LoadCsvData(path, @"Wikidata-stations-2025-01-24.tsv", '\t', Encoding.UTF8)
                     .Select(r => (ItemQ: r[0], Label: r[1], Latitude: r[3], Longitude: r[2], ID: r[4]))
                )
        {
            if (codebook.TryGetValue("CZ:" + row.ID.Substring(2), out var entry)
                && Single.TryParse(row.Latitude, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var latitude)
                && Single.TryParse(row.Longitude, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var longitude)
               )
            {
                entry.WikidataItem = row.ItemQ;
                if (entry.Latitude != null && entry.Longitude != null)
                {
                    var dist = Math.Abs(entry.Latitude.GetValueOrDefault() - latitude) + Math.Abs(entry.Longitude.GetValueOrDefault() - longitude);
                    if (dist > 0.005)
                    {
                        DebugLog.LogProblem(String.Format(CultureInfo.InvariantCulture, "Suspicious geographical position for point #{0} ({6}): {1}, {2} versus {3}, {4}: {5}", row.ID, latitude, longitude, entry.Latitude, entry.Longitude, dist * 40000.0f / 360.0f, row.ItemQ));
                        problematicPoints.Add(entry.FullIdentifier);
                    }
                }

                entry.Latitude = latitude;
                entry.Longitude = longitude;
            }
            else
            {
                DebugLog.LogProblem("Unknown point {0} from Wikidata", row.ID);
            }
        }

        /*
        foreach (var row in CodebookHelpers.LoadCsvData(path, @"osm-overpass-stations-2021-08-19.csv", '\t', Encoding.UTF8)
            .Select(r => (Latitude: r[0], Longitude: r[1], ID: r[2], Name: r[3]))
        )
        {
            if (codebook.TryGetValue("CZ:" + row.ID.Substring(0, 5), out var entry)
                && Single.TryParse(row.Latitude, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var latitude)
                && Single.TryParse(row.Longitude, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var longitude)
            )
            {
                if (entry.Latitude == null || entry.Longitude == null)
                {
                    entry.Latitude = latitude;
                    entry.Longitude = longitude;
                    DebugLog.LogDebugMsg("Added coordinates to {0}", row.ID);
                }
                else
                {
                    var dist = Math.Abs(entry.Latitude.GetValueOrDefault() - latitude) + Math.Abs(entry.Longitude.GetValueOrDefault() - longitude);
                    if (dist > 0.005 && !problematicPoints.Contains(entry.FullIdentifier))
                    {
                        DebugLog.LogProblem(String.Format(CultureInfo.InvariantCulture, "Suspicious geographical position for point #{0}: {1}, {2} versus OSM: {3}, {4}: {5}", row.ID, latitude, longitude, entry.Latitude, entry.Longitude, dist * 40000.0f / 360.0f));
                        problematicPoints.Add(entry.FullIdentifier);
                    }
                }
            }
            else
            {
                DebugLog.LogProblem("Unknown point {0} from OSM", row.ID);
            }
        }
        */

        DebugLog.LogDebugMsg("{0} point(s)", codebook.Count);

        var pointsWithWikidata = new Dictionary<PointType, Tuple<int, int>>(codebook.Count);
        foreach (var entry in codebook.Values.Where(value => value.Latitude != null))
        {
            if (String.IsNullOrEmpty(entry.WikidataItem) && (entry.Type == PointType.Stop || entry.Type == PointType.Station))
            {
                Console.WriteLine($"'{entry.LongName}' ({entry.ID}) {entry.Type} missing");
            }
            pointsWithWikidata.TryGetValue(entry.Type, out var currentCount);
            if (currentCount == null) currentCount = Tuple.Create(0, 0);
            if (String.IsNullOrEmpty(entry.WikidataItem)) currentCount = Tuple.Create(currentCount.Item1, currentCount.Item2 + 1);
            else currentCount = Tuple.Create(currentCount.Item1 + 1, currentCount.Item2);
            pointsWithWikidata[entry.Type] = currentCount;
        }
        Console.WriteLine("Wikidata statistics per point type:");
        foreach (var count in pointsWithWikidata)
        {
            Console.WriteLine($"{count.Key}: {count.Value.Item1} with Wikidata, {count.Value.Item2} without ({100.0m * count.Value.Item1 / (count.Value.Item1 + count.Value.Item2):N1} %)");
        }

        tree = new KdTree<float, string>(2, new FloatMath());
        foreach (var p in codebook.Where(p => p.Value.Latitude != null))
        {
            tree.Add([p.Value.Latitude.GetValueOrDefault(), p.Value.Longitude.GetValueOrDefault()], p.Key);
        }
    }

    public PointCodebookEntry Find(string id)
    {
        codebook.TryGetValue(id, out var result);
        return result;
    }

    public List<PointCodebookEntry> FindNearest(float latitude, float longitude, int neighbors)
    {
        return tree.GetNearestNeighbours([latitude, longitude], neighbors).Select(point => Find(point.Value)).Where(x => x != null).ToList();
    }

    private static PointType ParsePointType(string typeStr)
    {
        return !pointTypePerName.TryGetValue(typeStr.Trim(), out var type) ? PointType.Unknown : type;
    }

    private static float? ParseGeoCoordinate(string posStr)
    {
        if (String.IsNullOrEmpty(posStr)) return null;
        if (posStr == "E  °',   \"" || posStr == "N  °',   \"") return null;
        if (posStr == "E  °00'00,   \"" || posStr == "N  °00'00,   \"") return null;
        var match = regexGeoCoordinate.Match(posStr);
        if (!match.Success) throw new FormatException($"Invalid geographical coordinate '{posStr}'");
        var deg = ParseFloat(match.Groups["deg"].Value);
        var min = ParseFloat(match.Groups["min"].Value);
        var sec = ParseFloat(match.Groups["sec"].Value.Replace(',', '.'));
        return deg + (min / 60.0f) + (sec / 60.0f / 60.0f);
    }

    private static float ParseFloat(string str)
    {
        str = str.Replace(" ", "");
        if (str == "") return 0;
        if (str.StartsWith(".")) str = '0' + str;
        if (str.EndsWith(".")) str += '0';
        if (!Single.TryParse(str, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
        {
            throw new FormatException($"Invalid float number: {str}");
        }

        return result;
    }

    private static readonly Dictionary<string, PointType> pointTypePerName = new Dictionary<string, PointType>(StringComparer.OrdinalIgnoreCase)
    {
        { "Automatické hradlo", PointType.Point },
        { "Automatické hradlo a zastávka", PointType.Stop },
        { "Automatické hradlo, nákladiště a zastávka", PointType.Stop },
        { "Dopr.body na cizím území (blíže neurčené)", PointType.Point },
        { "Dopravna D3", PointType.Stop },
        { "Dopravna radiobloku", PointType.Stop },
        { "Hláska", PointType.Point },
        { "Hláska a zastávka", PointType.Stop },
        { "Hláska, nákladiště a zastávka", PointType.Stop },
        { "Hradlo", PointType.Point },
        { "Hradlo a zastávka", PointType.Stop },
        { "Hranice infrastruktur", PointType.InnerBoundary },
        { "Hranice oblastí", PointType.InnerBoundary },
        { "Hranice OPŘ totožná s hranicí VÚSC", PointType.InnerBoundary },
        { "Hranice TUDU (začátek nebo konec TUDU)", PointType.InnerBoundary },
        { "Hranice třídy sklonu", PointType.InnerBoundary },
        { "Jiné dopravní body", PointType.Point },
        { "Kolejová křižovatka", PointType.Crossing },
        { "Kolejová skupina stanice nebo jiného DVM", PointType.Point },
        { "Nákladiště", PointType.Point },
        { "Nákladiště a zastávka", PointType.Stop },
        { "Obvod DVM nebo staniční kolejová skupina se zastávkou", PointType.Stop },
        { "Odbočení ve stanici nebo v jiném DVM", PointType.Crossing },
        { "Odbočení vlečky", PointType.Crossing },
        { "Odbočka (dopravna s kolejovým rozvětvením)", PointType.Crossing },
        { "Odbočka (dopravna) a zastávka", PointType.Stop },
        { "Odbočka (dopravna), nákladiště a zastávka", PointType.Stop },
        { "Odstup nezavěšeného postrku (na širé trati)", PointType.Point },
        { "Samostatné kolejiště vlečky", PointType.Point },
        { "Samostatné tarif.místo v rámci stanice nebo jiného DVM", PointType.Point },
        { "Skok ve staničení", PointType.Point },
        { "Stanice", PointType.Station },
        { "Stanice (z přepravního hlediska blíže neurčená)", PointType.Station },
        { "Státní hranice", PointType.StateBoundary },
        { "Trasovací bod TUDU, SENA", PointType.Point },
        { "Výhybna", PointType.Siding },
        { "Zastávka", PointType.Stop },
        { "Zastávka lanové dráhy", PointType.Stop },
        { "Zastávka náhradní autobusové dopravy", PointType.Point },
        { "Zastávka v obvodu stanice", PointType.Stop },
        { "Závorářské stanoviště", PointType.Point }
    };
}