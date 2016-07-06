using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KdyPojedeVlak.Engine;
using Microsoft.AspNetCore.Hosting;

namespace KdyPojedeVlak
{
    public class Program
    {
        // TODO: Dependency injection
        public static KangoSchedule Schedule;

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
    }
}
