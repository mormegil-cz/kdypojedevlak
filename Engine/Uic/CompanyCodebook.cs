using System;
using System.Collections.Generic;
using System.Text;

namespace KdyPojedeVlak.Engine.Uic
{
    public class CompanyCodebook
    {
        private Dictionary<string, CompanyCodebookEntry> codebook;
        private readonly string path;

        public CompanyCodebook(string path)
        {
            this.path = path;
        }

        public void Load()
        {
            if (codebook != null) throw new InvalidOperationException("Already loaded");

            codebook = new Dictionary<string, CompanyCodebookEntry>();
            CodebookHelpers.LoadCsvData(path, @"uic-company-codes.tsv", '\t', Encoding.GetEncoding(1250))
                .IntoDictionary(codebook, r => r[0], r => new CompanyCodebookEntry
                {
                    ID = r[0],
                    ShortName = r[1],
                    LongName = r[2],
                    Country = r[3],
                    Web = r[4]
                });
        }

        public CompanyCodebookEntry Find(string id)
        {
            codebook.TryGetValue(id, out var result);
            return result;
        }
    }
}
