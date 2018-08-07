using System;
using System.Diagnostics;
using KdyPojedeVlak.Engine;
using KdyPojedeVlak.Engine.Djr;
using KdyPojedeVlak.Engine.SR70;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KdyPojedeVlak
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        [DebuggerNonUserCode]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            try
            {
                var scheduleVersionManager = new ScheduleVersionManager(@"App_Data");
                Program.ScheduleVersionInfo = scheduleVersionManager.TryUpdate().Result;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error updating schedule: {0}", ex.Message);
                throw;
            }

            Program.PointCodebook = new PointCodebook(@"App_Data");
            try
            {
                Program.PointCodebook.Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading SR70 codebook: {0}", ex.Message);
            }

            //Program.Schedule = new DjrSchedule(Program.ScheduleVersionInfo.CurrentPath);
            Program.Schedule = new DjrSchedule(@"App_Data\data-gvd2018_3\GVD2018_3.ZIP");
            try
            {
                Program.Schedule.Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading schedule: {0}", ex.Message);
                throw;
            }
        }
    }
}