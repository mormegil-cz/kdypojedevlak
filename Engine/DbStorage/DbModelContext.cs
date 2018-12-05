using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace KdyPojedeVlak.Engine.DbStorage
{
    public class DbModelContext : DbContext
    {
        public DbSet<TimetableYear> TimetableYears { get; set; }
        public DbSet<Train> Trains { get; set; }
        public DbSet<TrainTimetable> TrainTimetables { get; set; }
        public DbSet<RoutingPoint> RoutingPoints { get; set; }

        public DbModelContext(DbContextOptions<DbModelContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Train>()
                .HasIndex(o => o.Number).IsUnique();

            modelBuilder.Entity<TrainTimetable>()
                .HasIndex(o => new {o.Train, o.TimetableYear}).IsUnique();

            modelBuilder.Entity<TrainTimetable>()
                .HasIndex(o => o.Name); // TODO: Fulltext

            modelBuilder.Entity<RoutingPoint>()
                .HasIndex(o => o.Code).IsUnique();

            modelBuilder.Entity<RoutingPoint>()
                .HasIndex(o => new {o.Latitude, o.Longitude}); // TODO: Geographic coordinates (R-Tree)

            modelBuilder.Entity<Passage>()
                .HasIndex(o => new {o.Year, o.Point, o.Train, o.Order}).IsUnique();
            modelBuilder.Entity<Passage>()
                .HasIndex(o => new {o.Year, o.Train, o.Order}).IsUnique();
            modelBuilder.Entity<Passage>()
                .HasIndex(o => new {o.Year, o.Point, o.ArrivalTime});

            modelBuilder.Entity<NeighboringPoints>()
                .HasKey(o => new {o.PointA, o.PointB});
        }
    }

    public class TimetableYear
    {
        [Key] public int Year { get; set; }

        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
    }


    public class CalendarDefinition
    {
        public int Id { get; set; }

        [Required] public TimetableYear TimetableYear { get; set; }
        [Required] public string Guid { get; set; }
        [Required] public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool[] Bitmap { get; set; }

        public Guid ComputeGuid()
        {
            var start = StartDate;
            for (int i = 0; i < Bitmap.Length - 1; ++i, start = start.AddDays(1))
            {
                if (Bitmap[i])
                {
                    return ComputeGuid(start, Bitmap, i + 1);
                }
            }

            return System.Guid.Empty;
        }

        private static Guid ComputeGuid(DateTime startDate, bool[] bitmap, int startIndex)
        {
            return BuildGuidFromHash(String.Format("{0}|{1}", startDate.ToString("o", CultureInfo.InvariantCulture),
                bitmap.Skip(startIndex).Aggregate(new StringBuilder(bitmap.Length - startIndex), (sb, value) =>
                {
                    sb.Append(value ? '1' : '0');
                    return sb;
                }).ToString()));
        }

        private static Guid BuildGuidFromHash(string data)
        {
            throw new NotImplementedException();
        }
    }

    public class RoutingPoint
    {
        public int Id { get; set; }

        [Required] public string Code { get; set; }
        public string Name { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    public class Train
    {
        public int Id { get; set; }

        public int Number { get; set; }
    }

    public class TrainTimetable
    {
        public int Id { get; set; }

        [Required] public Train Train { get; set; }
        [Required] public TimetableYear TimetableYear { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Data { get; set; }

        public List<TrainTimetableVariant> Variants { get; set; }
    }

    public class TrainTimetableVariant
    {
        public int Id { get; set; }

        [Required] public TrainTimetable Timetable { get; set; }
        [Required] public CalendarDefinition Calendar { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    public class Passage
    {
        public int Id { get; set; }

        [Required] public TimetableYear Year { get; set; }
        [Required] public RoutingPoint Point { get; set; }
        [Required] public Train Train { get; set; }
        public int Order { get; set; }

        public TimeSpan ArrivalTime { get; set; }
        public TimeSpan? DepartureTime { get; set; }

        public Dictionary<string, string> Data { get; set; }
    }

    public class NeighboringPoints
    {
        [Required] public RoutingPoint PointA { get; set; }
        [Required] public RoutingPoint PointB { get; set; }
    }
}