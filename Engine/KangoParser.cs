using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdyPojedeVlak.Engine
{
    public class TrainCalendar
    {
        public string ID { get; set; }
        public string Description { get; set; }
        public bool[] Bitmap { get; set; }
    }

    public class TrainPassInfo
    {
        public TimeSpan ScheduledTime { get; private set; }
        public string TrainNumber { get; private set; }
        public string TrainType { get; private set; }
        public string TrainName { get; private set; }
        public TrainCalendar Calendar { get; private set; }
    }

    public class KangoParser
    {
        private readonly string path;

        public KangoParser(string path)
        {
            this.path = path;
        }

        public IEnumerable<TrainPassInfo> GetPassesThrough(string point)
        {

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

        private static int? GetNumberFromRow(string[] row, int col1, int col2, bool reportErrors)
        {
            int result;
            if (String.IsNullOrEmpty(row[col1]))
            {
                if (String.IsNullOrEmpty(row[col2]))
                {
                    if (reportErrors) Console.WriteLine("No data at {0}, {1}", col1, col2);
                    return null;
                }
                if (!Int32.TryParse(row[col2], out result))
                {
                    if (reportErrors) Console.WriteLine("Bad data at {0}: '{1}'", col2, row[col2]);
                    return null;
                }
            }
            else
            {
                if (!Int32.TryParse(row[col1], out result))
                {
                    if (reportErrors) Console.WriteLine("Bad data at {0}: '{1}'", col1, row[col1]);
                    return null;
                }
            }
            return result;
        }

        private static TimeSpan? GetTimeFromRow(string[] row)
        {
            var dd = GetNumberFromRow(row, 7, 13, false);

            var hh = GetNumberFromRow(row, 8, 14, true);
            if (hh == null) return null;

            var mm = GetNumberFromRow(row, 9, 15, true);
            if (mm == null) return null;

            var ss = GetNumberFromRow(row, 10, 16, false);

            return new TimeSpan(dd.GetValueOrDefault(), hh.GetValueOrDefault(), mm.GetValueOrDefault(), ss.GetValueOrDefault() * 30);
        }
    }
}
