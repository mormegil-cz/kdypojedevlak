using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using KdyPojedeVlak.Engine;
using KdyPojedeVlak.Engine.Algorithms;
using KdyPojedeVlak.Engine.Djr;
using KdyPojedeVlak.Engine.Djr.DjrXmlModel;
using KdyPojedeVlak.Engine.SR70;
using Microsoft.AspNetCore.Hosting;

namespace KdyPojedeVlak
{
    public class Program
    {
        // TODO: Dependency injection
        //public static KangoSchedule Schedule;
        [Obsolete]
        public static DjrSchedule Schedule;
        public static ScheduleVersionInfo ScheduleVersionInfo;
        public static PointCodebook PointCodebook;

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();

        private static void TestMerge<T>(params List<T>[] lists)
        {
            var merged = ListMerger.MergeLists(lists.ToList());
            foreach (var list in lists)
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    var iIndex = merged.IndexOf(list[i]);
                    AssertTrue(iIndex >= 0);
                    for (var j = i + 1; j < list.Count; ++j)
                    {
                        var jIndex = merged.IndexOf(list[j]);
                        AssertTrue(jIndex >= 0);
                        AssertTrue(iIndex < jIndex);
                    }
                }
            }
        }

        private static void AssertTrue(bool test)
        {
            if (!test) throw new Exception("Assertion failed");
        }

        private static List<T> L<T>(params T[] items)
        {
            return items.ToList();
        }


        private static void TestSerialize()
        {
            var ser = new XmlSerializer(typeof(CZPTTCISMessage));
            ser.Serialize(Console.Out, new CZPTTCISMessage
            {
                Identifiers = new Identifiers
                {
                    PlannedTransportIdentifiers = new[]
                    {
                        new PlannedTransportIdentifiers {ObjectType = "PA", Company = "0054"},
                        new PlannedTransportIdentifiers {ObjectType = "TR", Company = "3246"},
                    }.ToList()
                },
                CZPTTCreation = DateTime.Now,
                CZPTTInformation = new CZPTTInformation
                {
                    PlannedCalendar = new PlannedCalendar
                    {
                        BitmapDays = "1111111111111111111111111111111111111111111111111111111111111111111111111111111111111",
                        ValidityPeriod = new ValidityPeriod
                        {
                            StartDateTime = new DateTime(2018, 06, 10),
                            EndDateTime = new DateTime(2018, 09, 2)
                        }
                    },
                    CZPTTLocation = new[]
                    {
                        new CZPTTLocation
                        {
                            JourneyLocationTypeCode = "01"
                        }
                    }.ToList()
                }
            });
        }
    }
}