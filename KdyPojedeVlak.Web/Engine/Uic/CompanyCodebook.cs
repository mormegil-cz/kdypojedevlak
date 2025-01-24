using System;
using System.Collections.Generic;
using System.Text;

namespace KdyPojedeVlak.Web.Engine.Uic;

public class CompanyCodebook(string path)
{
    private Dictionary<string, CompanyCodebookEntry> codebook;

    public void Load()
    {
        if (codebook != null) throw new InvalidOperationException("Already loaded");

        codebook = new Dictionary<string, CompanyCodebookEntry>();
        CodebookHelpers.LoadCsvData(path, @"uic-company-codes.tsv", '\t', Encoding.UTF8)
            .IntoDictionary(codebook, r => r[0], r => new CompanyCodebookEntry
            {
                ID = r.Length < 3 ? throw new FormatException(String.Join(" || ", r)) : r[0],
                ShortName = r[1],
                LongName = r.Length >= 3 ? r[2] : r[1],
                Country = r.Length >= 4 ? r[3] : null,
                Web = r.Length >= 5 ? NormalizeUrl(r[4]) : null
            });
    }

    private static string NormalizeUrl(string urlStr)
    {
        var basicUrlStr = urlStr.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) ? urlStr : "http://" + urlStr;
        return Uri.TryCreate(basicUrlStr, UriKind.Absolute, out _) ? basicUrlStr : null;
    }

    public CompanyCodebookEntry Find(string id)
    {
        codebook.TryGetValue(id, out var result);
        return result;
    }
}