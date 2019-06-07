using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using KdyPojedeVlak.Engine.Djr;
using KdyPojedeVlak.Engine.SR70;
using Microsoft.EntityFrameworkCore;

namespace KdyPojedeVlak.Engine.DbStorage
{
    public class DbModelContext : DbContext
    {
        public DbSet<TimetableYear> TimetableYears { get; set; }
        public DbSet<Train> Trains { get; set; }
        public DbSet<TrainTimetable> TrainTimetables { get; set; }
        public DbSet<RoutingPoint> RoutingPoints { get; set; }
        public DbSet<CalendarDefinition> CalendarDefinitions { get; set; }
        public DbSet<NeighboringPoints> NeighboringPointTuples { get; set; }

        public DbModelContext(DbContextOptions<DbModelContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Train>()
                .HasIndex(o => o.Number).IsUnique();

            modelBuilder.Entity<TrainTimetable>()
                .HasIndex(o => new {o.TrainId, o.YearId}).IsUnique();

            modelBuilder.Entity<TrainTimetable>()
                .HasIndex(o => o.Name); // TODO: Fulltext

            modelBuilder.Entity<RoutingPoint>()
                .HasIndex(o => o.Code).IsUnique();

            modelBuilder.Entity<RoutingPoint>()
                .HasIndex(o => new {o.Latitude, o.Longitude}); // TODO: Geographic coordinates (R-Tree)

            modelBuilder.Entity<Passage>()
                .HasIndex(o => new {o.PointId, o.TrainId, o.Order}).IsUnique();
            modelBuilder.Entity<Passage>()
                .HasIndex(o => new {o.TrainId, o.Order}).IsUnique();
            modelBuilder.Entity<Passage>()
                .HasIndex(o => new {o.YearId, o.PointId, o.ArrivalTime});

            modelBuilder.Entity<NeighboringPoints>()
                .HasKey(o => new {o.PointAId, o.PointBId});
        }
    }

    public class TimetableYear
    {
        [Key]
        public int Year { get; set; }

        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
    }

    public class CalendarDefinition
    {
        private static readonly Guid calendarNamespaceGuid = new Guid("4eb0c41b-32a1-4acb-bd5f-8a6dbe847162");

        private bool[] bitmap;

        public int Id { get; set; }

        [Required]
        public TimetableYear TimetableYear { get; set; }

        [Required]
        public Guid Guid { get; set; }

        [Required]
        [MaxLength(200)]
        public string Description { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(70)]
        public String BitmapEncoded { get; set; }

        [NotMapped]
        // TODO: Decode BitmapEncoded
        public bool[] Bitmap
        {
            get
            {
                if (bitmap == null)
                {
                    var bytes = Convert.FromBase64String(BitmapEncoded);
                    var decoded = new bool[bytes.Length * 8];
                    new BitArray(bytes).CopyTo(decoded, 0);
                    bitmap = decoded;
                }

                return bitmap;
            }
            set
            {
                bitmap = value;
                var bytes = new byte[(bitmap.Length + 7) / 8];
                new BitArray(bitmap).CopyTo(bytes, 0);
                BitmapEncoded = Convert.ToBase64String(bytes);
                Guid = ComputeGuid();
            }
        }

        private Guid ComputeGuid()
        {
            var start = StartDate;
            var bits = bitmap;
            for (int i = 0; i < bits.Length - 1; ++i, start = start.AddDays(1))
            {
                if (bits[i])
                {
                    return ComputeGuid(start, bits, i + 1);
                }
            }

            return Guid.Empty;
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
            return new Guid(GuidEx.NewGuid(data, calendarNamespaceGuid).ToByteArray());
        }
    }

    public class RoutingPoint
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public float? Latitude { get; set; }
        public float? Longitude { get; set; }

        public string DataJson { get; set; }

        [NotMapped]
        // TODO: Decode data JSON
        public Dictionary<string, string> Data { get; set; }

        // TODO: Point attributes
        [NotMapped]
        public PointType Type => Program.PointCodebook.Find(Code)?.Type ?? PointType.Unknown;

        [NotMapped]
        public String ShortName => Program.PointCodebook.Find(Code)?.ShortName ?? Name;

        [InverseProperty("Point")]
        public List<Passage> PassingTrains { get; set; }
    }

    /**
     * Label of a train, common for all timetable years
     */
    public class Train
    {
        public int Id { get; set; }

        public string Number { get; set; }
    }

    /**
     * Train in a single timetable year
     */
    public class TrainTimetable
    {
        public int Id { get; set; }

        [Required]
        public int TrainId { get; set; }

        [ForeignKey("TrainId")]
        public Train Train { get; set; }

        [Required]
        public int YearId { get; set; }

        [ForeignKey("YearId")]
        public TimetableYear TimetableYear { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public string DataJson { get; set; }

        [InverseProperty("Timetable")]
        public virtual List<TrainTimetableVariant> Variants { get; set; }

        [NotMapped]
        // TODO: Decode data JSON
        public Dictionary<string, string> Data { get; set; }

        [NotMapped]
        public string TrainNumber => Train.Number;

        // TODO: Train attributes
        [NotMapped]
        public TrainCategory TrainCategory => TrainCategory.Unknown;
    }

    /**
     * Variant of a train in the respective timetable year
     */
    public class TrainTimetableVariant
    {
        public int Id { get; set; }

        [Required]
        public TrainTimetable Timetable { get; set; }

        [Required]
        public CalendarDefinition Calendar { get; set; }

        public string DataJson { get; set; }

        [NotMapped]
        // TODO: Decode data JSON
        public Dictionary<string, string> Data { get; set; }

        public virtual List<Passage> Points { get; set; }
    }

    public class Passage
    {
        public int Id { get; set; }

        // note that this is denormalized: TrainId implies YearId, but it would be difficult to normalize it
        // and we need YearId to properly find all transits through a given point (in the specific timetable year)
        [Required]
        public int YearId { get; set; }

        [ForeignKey("YearId")]
        public TimetableYear Year { get; set; }

        [Required]
        public int PointId { get; set; }

        [ForeignKey("PointId")]
        public RoutingPoint Point { get; set; }

        [Required]
        public int TrainId { get; set; }

        [ForeignKey("TrainId")]
        public TrainTimetableVariant TrainTimetableVariant { get; set; }

        public int Order { get; set; }

        public TimeSpan? ArrivalTime { get; set; }
        public TimeSpan? DepartureTime { get; set; }
        public decimal? DwellTime { get; set; }

        public int ArrivalDay { get; set; }
        public int DepartureDay { get; set; }

        public string DataJson { get; set; }

        [NotMapped]
        // TODO: Decode data JSON
        public Dictionary<string, string> Data { get; set; }

        [NotMapped]
        public bool IsMajorPoint => DwellTime != null;

        // TODO: Point attributes
        [NotMapped]
        public string SubsidiaryLocationDescription => null;

        [NotMapped]
        public List<TrainOperation> TrainOperations => Enumerable.Empty<TrainOperation>().ToList();
    }

    public class NeighboringPoints
    {
        [Required]
        public int PointAId { get; set; }

        [Required]
        public int PointBId { get; set; }

        [ForeignKey("PointAId")]
        public RoutingPoint PointA { get; set; }

        [Required]
        [ForeignKey("PointBId")]
        public RoutingPoint PointB { get; set; }
    }
}