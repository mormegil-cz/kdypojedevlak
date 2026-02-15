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
        using var stream = new FileStream(Path.Combine(path, fileName), FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, encoding);

        string? line;
        var firstLine = true;
        while ((line = reader.ReadLine()) != null)
        {
            if (firstLine)
            {
                firstLine = false;
                continue;
            }

            // TODO: Real CSV processing
            if (line.Contains('"'))
            {
                yield return SplitCsvLine(line, fieldSeparator).ToArray();
            }
            else
            {
                yield return line.Split(fieldSeparator);
            }
        }
    }

    private static IEnumerable<string> SplitCsvLine(string line, char fieldSeparator)
    {
        var insideQuotes = false;
        var currStart = 0;
        for (var i = 0; i < line.Length; ++i)
        {
            var c = line[i];
            if (insideQuotes)
            {
                if (c == '"')
                {
                    if (i < line.Length - 1 && line[i + 1] == '"')
                    {
                        ++i;
                        continue;
                    }

                    insideQuotes = false;
                    if (i < line.Length - 1 && line[i + 1] != fieldSeparator) throw new FormatException("Unsupported partially quoted field at " + line);
                    yield return line[currStart..i].Replace("\"\"", "\"");;
                    ++i;
                    currStart = i + 1;
                }
            }
            else
            {
                if (c == fieldSeparator)
                {
                    yield return line[currStart..i];
                    currStart = i + 1;
                }
                else if (c == '"')
                {
                    if (currStart != i) throw new FormatException("Unsupported partially quoted field at " + line);
                    insideQuotes = true;
                    currStart = i + 1;
                }
            }
        }
        if (insideQuotes) throw new FormatException("Unsupported line break inside quoted string at " + line);
        yield return line[currStart..];
    }
}