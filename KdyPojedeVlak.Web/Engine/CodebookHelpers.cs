using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KdyPojedeVlak.Web.Engine;

public static class CodebookHelpers
{
    public static IEnumerable<string[]> LoadCsvData(string path, string fileName, char fieldSeparator, Encoding encoding)
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
}