using System;
using System.Diagnostics;
using System.IO;
using KdyPojedeVlak.Web.Engine;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Djr;
using KdyPojedeVlak.Web.Engine.SR70;
using KdyPojedeVlak.Web.Engine.Uic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KdyPojedeVlak.Web
{
    public class Startup
    {
        private static readonly bool RecreateDatabase = false;
        private static readonly bool EnableUpdates = true;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /*
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        */

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // services.Configure<CookiePolicyOptions>(options =>
            // {
            //     // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            //     options.CheckConsentNeeded = context => true;
            //     options.MinimumSameSitePolicy = SameSiteMode.None;
            // });


            services.AddControllersWithViews();

            // Add framework services.
            services.AddMvc();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(Configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
            });

            services.AddDbContext<DbModelContext>(
                options => options
                    .UseSqlite(Configuration.GetConnectionString("Database"))
                    .ConfigureWarnings(w => w.Log(RelationalEventId.MultipleCollectionIncludeWarning))
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        [DebuggerNonUserCode]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            var serviceScopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>();

            Program.PointCodebook = new PointCodebook(@"App_Data");
            try
            {
                Program.PointCodebook.Load();
            }
            catch (Exception ex)
            {
                DebugLog.LogProblem("Error loading SR70 codebook: {0}", ex);
                throw;
            }

            Program.CompanyCodebook = new CompanyCodebook(@"App_Data");
            try
            {
                Program.CompanyCodebook.Load();
            }
            catch (Exception ex)
            {
                DebugLog.LogProblem("Error loading company codebook: {0}", ex);
                throw;
            }

            try
            {
                using var serviceScope = serviceScopeFactory.CreateScope();
                using var dbModelContext = serviceScope.ServiceProvider.GetRequiredService<DbModelContext>();

                if (RecreateDatabase)
                {
                    dbModelContext.Database.EnsureDeleted();
                    dbModelContext.Database.EnsureCreated();
                }
                DebugLog.LogDebugMsg("Migrating database");
                dbModelContext.Database.Migrate();
                DebugLog.LogDebugMsg("Migration completed");

                ScheduleVersionInfo.Initialize(dbModelContext);

                dbModelContext.SaveChanges();

                dbModelContext.Database.ExecuteSqlRaw("PRAGMA optimize");
            }
            catch (Exception ex)
            {
                DebugLog.LogProblem("Error initializing database: {0}", ex);
                throw;
            }

            if (EnableUpdates && Configuration["DisableUpdates"] == null) UpdateManager.Initialize(Path.Combine("App_Data", "cisjrdata"), serviceScopeFactory);
        }
    }
}