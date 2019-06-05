using System;
using System.Diagnostics;
using KdyPojedeVlak.Engine;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Engine.Djr;
using KdyPojedeVlak.Engine.SR70;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Add framework services.
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddDbContext<DbModelContext>(
                options => options.UseSqlite(Configuration.GetConnectionString("Database")));
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
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

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
            catch (Exception ex)
            {
                DebugLog.LogProblem("Error updating schedule: {0}", ex.Message);
                throw;
            }

            Program.PointCodebook = new PointCodebook(@"App_Data");
            try
            {
                Program.PointCodebook.Load();
            }
            catch (Exception ex)
            {
                DebugLog.LogProblem("Error loading SR70 codebook: {0}", ex.Message);
            }

            Program.Schedule = new DjrSchedule(Program.ScheduleVersionInfo.CurrentPath);
            try
            {
                //Program.Schedule.Load();

                using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
                {
                    var context = serviceScope.ServiceProvider.GetRequiredService<DbModelContext>();
                    //context.Database.EnsureDeleted();
                    context.Database.EnsureCreated();

                    context.ChangeTracker.AutoDetectChangesEnabled = false;

                    //Program.Schedule.StoreToDatabase(context);

                    //context.SaveChanges();
                }

                //Program.Schedule.ClearTemps();
            }
            catch (Exception ex)
            {
                DebugLog.LogProblem("Error loading schedule: {0}", ex.Message);
                throw;
            }
        }
    }
}