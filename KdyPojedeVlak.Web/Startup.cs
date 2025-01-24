using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using KdyPojedeVlak.Web.Engine;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.SR70;
using KdyPojedeVlak.Web.Engine.Uic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KdyPojedeVlak.Web;

public class Startup(IConfiguration configuration)
{
    private const bool RecreateDatabase = false;
    private const bool EnableUpdates = true;

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
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
            loggingBuilder.AddConsole();
            loggingBuilder.AddDebug();
        });

        services.AddDbContext<DbModelContext>(
            options => options
                .UseSqlite(configuration.GetConnectionString("Database"))
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

        Program.PointCodebook = new PointCodebook(configuration["PointCodebookLocation"] ?? "App_Data");
        try
        {
            Program.PointCodebook.Load();
        }
        catch (Exception ex)
        {
            DebugLog.LogProblem("Error loading SR70 codebook: {0}", ex);
            throw;
        }

        Program.CompanyCodebook = new CompanyCodebook(configuration["CompanyCodebookLocation"] ?? "App_Data");
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
            if (dbModelContext.Database.GetPendingMigrations().Any())
            {
                DebugLog.LogDebugMsg("Migrating database");
                dbModelContext.Database.Migrate();
                DebugLog.LogDebugMsg("Vacuuming database");
                dbModelContext.Database.ExecuteSqlRaw("VACUUM");
                DebugLog.LogDebugMsg("Migration completed");
            }

            ScheduleVersionInfo.Initialize(dbModelContext);

            dbModelContext.SaveChanges();

            dbModelContext.Database.ExecuteSqlRaw("PRAGMA optimize");
        }
        catch (Exception ex)
        {
            DebugLog.LogProblem("Error initializing database: {0}", ex);
            throw;
        }

        if (EnableUpdates && configuration["DisableUpdates"] == null) UpdateManager.Initialize(configuration["CisJrFilesLocation"] ?? Path.Combine("App_Data", "cisjrdata"), serviceScopeFactory);
    }
}