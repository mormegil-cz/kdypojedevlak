using System;
using System.Linq;
using System.Xml.Serialization;
using KdyPojedeVlak.Engine;
using KdyPojedeVlak.Engine.Djr;
using KdyPojedeVlak.Engine.Djr.DjrXmlModel;
using KdyPojedeVlak.Engine.Kango;

namespace KdyPojedeVlak
{
    public class Program
    {
        // TODO: Dependency injection
        public static KangoSchedule Schedule;
        public static ScheduleVersionInfo ScheduleVersionInfo;

        public static void Main(string[] args)
        {
//            TestSerialize();
//            return;
            try
            {
                new DjrSchedule(@"c:\Users\Petr\LinuxShare\GVD2018_3.ZIP").Load();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

/*
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        */
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
                        new PlannedTransportIdentifiers { ObjectType = "PA", Company = "0054" },
                        new PlannedTransportIdentifiers { ObjectType = "TR", Company = "3246" },
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
                            StartDateTime = new DateTimeOffset(2018, 06, 10, 0, 0, 0, TimeSpan.Zero).ToString("o"),
                            EndDateTime = new DateTimeOffset(2018, 09, 2, 0, 0, 0, TimeSpan.Zero).ToString("o")
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