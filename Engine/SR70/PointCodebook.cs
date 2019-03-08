using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Supercluster.KDTree;

namespace KdyPojedeVlak.Engine.SR70
{
    public class PointCodebook
    {
        private static Regex regexGeoCoordinate = new Regex(@"\s*^[NE]\s*(?<deg>[0-9]+)\s*°\s*(?<min>[0-9]+)\s*'\s*(?<sec>[0-9]+\s*(,\s*([0-9]+)?)?)\s*""\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        private readonly string path;
        private Dictionary<string, PointCodebookEntry> codebook;
        private KDTree<float, string> tree;

        static PointCodebook()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public PointCodebook(string path)
        {
            this.path = path;
        }

        public void Load()
        {
            if (codebook != null) throw new InvalidOperationException("Already loaded");

            codebook = new Dictionary<string, PointCodebookEntry>();
            LoadCsvData(path, @"SR70-2019-01-01.csv", ';', Encoding.GetEncoding(1250))
                .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r))
                .IntoDictionary(codebook, r => r.ID, r => new PointCodebookEntry
                {
                    ID = r.Row[0],
                    LongName = r.Row[1],
                    ShortName = r.Row[2],
                    Type = ParsePointType(r.Row[6]),
                    Longitude = ParseGeoCoordinate(r.Row[15]),
                    Latitude = ParseGeoCoordinate(r.Row[16]),
                });

            foreach (var row in LoadCsvData(path, @"osm-overpass-stations-2019-03-08.csv", '\t', Encoding.UTF8)
                .Skip(1)
                .Select(r => (Latitude: r[0], Longitude: r[1], ID: r[2], Name: r[3]))
            )
            {
                if (codebook.TryGetValue("CZ:" + row.ID.Substring(0, 5), out var entry)
                    && Single.TryParse(row.Latitude, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var latitude)
                    && Single.TryParse(row.Longitude, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var longitude)
                )
                {
                    /*
                    if (entry.Latitude != null || entry.Longitude != null)
                    {
                        DebugLog.LogProblem("Duplicate geographical position for point #{0}", row.ID);
                        continue;
                    }

                    entry.Latitude = latitude;
                    entry.Longitude = longitude;
                    ++pointsWithPositions;
                    */

                    var dist = Math.Abs(entry.Latitude.GetValueOrDefault() - latitude) + Math.Abs(entry.Longitude.GetValueOrDefault() - longitude);
                    if (dist > 0.005)
                    {
                        DebugLog.LogProblem(String.Format(CultureInfo.InvariantCulture, "Suspicious geographical position for point #{0}: {1}, {2} versus {3}, {4}: {5}", row.ID, latitude, longitude, entry.Latitude, entry.Longitude, dist * 40000.0f / 360.0f));
                    }
                }
            }

            DebugLog.LogDebugMsg("{0} point(s)", codebook.Count);

            var pointList = codebook.Where(p => p.Value.Latitude != null).ToList();
            var pointIDs = pointList.Select(p => p.Key).ToArray();
            var pointCoordinates = pointList.Select(p => new[] {p.Value.Latitude.GetValueOrDefault(), p.Value.Longitude.GetValueOrDefault()}).ToArray();
            tree = new KDTree<float, string>(2, pointCoordinates, pointIDs, L2Norm);
        }

        private static double L2Norm(float[] x, float[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }

            return dist;
        }

        public PointCodebookEntry Find(string id)
        {
            codebook.TryGetValue(id, out var result);
            return result;
        }

        public List<PointCodebookEntry> FindNearest(float latitude, float longitude)
        {
            return tree.NearestNeighbors(new[] {latitude, longitude}, 5).Select(point => Find(point.Item2)).Where(x => x != null).ToList();
        }

        private static PointType ParsePointType(string typeStr)
        {
            return !pointTypePerName.TryGetValue(typeStr, out var type) ? PointType.Unknown : type;
        }

        private static float? ParseGeoCoordinate(string posStr)
        {
            if (String.IsNullOrEmpty(posStr)) return null;
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
            if (str.EndsWith(".")) str += '0';
            if (!Single.TryParse(str, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
            {
                throw new FormatException($"Invalid float number: {str}");
            }

            return result;
        }

        private static IEnumerable<string[]> LoadCsvData(string path, string fileName, char fieldSeparator, Encoding encoding)
        {
            using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(stream, encoding))
                {
                    string line;
                    bool firstLine = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // TODO: Real CSV processing
                        if (line.Contains('"'))
                        {
                            yield return line.Split(fieldSeparator).Select(field =>
                            {
                                if (!field.Contains('"')) return field;
                                if (field.Length < 2 || field[0] != '"' || field[field.Length - 1] != '"') throw new FormatException($"Invalid or unsupported CSV file: '{field}' at '{line}'");
                                if (field.Count(c => c == '"') % 2 != 0) throw new FormatException($"Unsupported CSV file: '{field}' at '{line}'");
                                return field.Substring(1, field.Length - 2).Replace("\"\"", "\"");
                            }).ToArray();
                        }
                        else
                        {
                            if (firstLine)
                            {
                                firstLine = false;
                                continue;
                            }

                            yield return line.Split(fieldSeparator);
                        }
                    }
                }
            }
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
            {"Kolejová skupina stanice nebo jiného DVM", PointType.Point},
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