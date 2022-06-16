#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Djr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KdyPojedeVlak.Web.Engine
{
    public class UpdateManager
    {
        private const int WakeupInterval = 60 * 60 * 1000;
        private const int InitialDelay = 1 * 20 * 1000;

        private static UpdateManager? instance;

        private readonly string basePath;

//        private readonly int initialDelay;
//        private readonly int wakeupInterval;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly Thread thread;
        private readonly object sync = new object();

        private volatile bool terminated;

        public static void Initialize(string basePath, IServiceScopeFactory serviceScopeFactory)
        {
            instance = new UpdateManager(basePath, serviceScopeFactory);
            instance.DoStart();
        }

        public static void Stop()
        {
            instance?.DoStop();
        }

        private UpdateManager(string basePath, IServiceScopeFactory serviceScopeFactory)
        {
            this.basePath = basePath;
            this.serviceScopeFactory = serviceScopeFactory;
            thread = new Thread(Run) { IsBackground = true };
        }

        private void DoStart()
        {
            thread.Start();
        }

        private void DoStop()
        {
            terminated = true;
            lock (sync)
            {
                Monitor.Pulse(sync);
            }
        }

        private void Run()
        {
            if (terminated) return;
            lock (sync)
            {
                Monitor.Wait(sync, InitialDelay);
            }

            while (!terminated)
            {
                Dictionary<string, long> availableDataFiles;
                try
                {
                    availableDataFiles = CisjrUpdater.DownloadMissingFiles(basePath).Result;
                }
                catch (Exception ex)
                {
                    DebugLog.LogProblem("Error downloading new schedule files: {0}", ex);
                    return;
                }

                try
                {
                    using var serviceScope = serviceScopeFactory.CreateScope();
                    using var context = serviceScope.ServiceProvider.GetRequiredService<DbModelContext>();

                    DjrSchedule.ImportNewFiles(context, availableDataFiles);

                    context.SaveChanges();

                    context.Database.ExecuteSqlRaw("PRAGMA optimize");
                }
                catch (Exception ex)
                {
                    DebugLog.LogProblem("Error loading schedule: {0}", ex);
                    return;
                }

                lock (sync)
                {
                    Monitor.Wait(sync, WakeupInterval);
                }
            }
        }
    }
}