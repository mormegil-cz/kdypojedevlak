﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KdyPojedeVlak.Engine.SR70
{
    public class PointCodebook
    {
        private readonly string path;
        private Dictionary<string, PointCodebookEntry> codebook;

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
            LoadCsvData(path, @"SR70-2017-12-10.csv")
                .Select(r => (ID: "CZ:" + r[0].Substring(0, r[0].Length - 1), Row: r))
                .IntoDictionary(codebook, r => r.ID, r => new PointCodebookEntry
                {
                    ID = r.Row[0],
                    LongName = r.Row[1],
                    ShortName = r.Row[2],
                    Type = ParsePointType(r.Row[5])
                });
        }

        public PointCodebookEntry Find(string id)
        {
            codebook.TryGetValue(id, out var result);
            return result;
        }

        private static PointType ParsePointType(string typeStr)
        {
            PointType type;
            return !pointTypePerName.TryGetValue(typeStr, out type) ? PointType.Unknown : type;
        }

        private static IEnumerable<string[]> LoadCsvData(string path, string fileName)
        {
            using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(stream, Encoding.GetEncoding(1250)))
                {
                    string line;
                    bool firstLine = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        /*
                        TODO: Real CSV processing
                        if (line.Contains('"'))
                        {
                        }
                        else
                        */
                        {
                            if (firstLine)
                            {
                                firstLine = false;
                                continue;
                            }
                            yield return line.Split(';');
                        }
                    }
                }
            }
        }

        private static readonly Dictionary<string, PointType> pointTypePerName = new Dictionary<string, PointType>(StringComparer.OrdinalIgnoreCase)
        {
            { "Automatické hradlo", PointType.Point },
            { "Automatické hradlo a zastávka", PointType.Stop },
            { "Automatické hradlo, nákladiště a zastávka", PointType.Stop },
            { "Dopr.body na cizím území (blíže neurčené)", PointType.Point },
            { "Dopravna D3", PointType.Point },
            { "Dopravna radiobloku", PointType.Point },
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
            { "Kolejová skupina stanice nebo jiného DVM", PointType.Station },
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
            { "Stanice (z přepravního hlediska blíže neurčená)", PointType.Station },
            { "Státní hranice", PointType.StateBoundary },
            { "Trasovací bod TUDU, SENA", PointType.Point },
            { "Výhybna", PointType.Siding },
            { "Zastávka", PointType.Stop },
            { "Zastávka lanové dráhy", PointType.Stop },
            { "Zastávka náhradní autobusové dopravy", PointType.Stop },
            { "Zastávka v obvodu stanice", PointType.Stop },
            { "Závorářské stanoviště", PointType.Point }
        };
    }
}