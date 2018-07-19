using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using KdyPojedeVlak.Engine;
using KdyPojedeVlak.Engine.Djr;
using KdyPojedeVlak.Engine.Djr.DjrXmlModel;
using KdyPojedeVlak.Engine.Kango;
using Microsoft.AspNetCore.Hosting;

namespace KdyPojedeVlak
{
    public class Program
    {
        // TODO: Dependency injection
        //public static KangoSchedule Schedule;
        public static DjrSchedule Schedule;
        public static ScheduleVersionInfo ScheduleVersionInfo;

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
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