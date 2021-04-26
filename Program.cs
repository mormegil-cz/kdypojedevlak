using System.IO;
using KdyPojedeVlak.Engine.SR70;
using KdyPojedeVlak.Engine.Uic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace KdyPojedeVlak
{
    public class Program
    {
        // TODO: Dependency injection
        public static PointCodebook PointCodebook;
        public static CompanyCodebook CompanyCodebook;

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        /*
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
        */
    }
}