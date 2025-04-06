//#nullable enable

using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using KdyPojedeVlak.Web.Engine.Djr;
using KdyPojedeVlak.Web.Engine.SR70;
using KdyPojedeVlak.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace KdyPojedeVlak.Web.Engine.DbStorage;

using static DbModelUtils;

public class DbModelContext(DbContextOptions<DbModelContext> options) : DbContext(options)
{
    public DbSet<ImportedFile> ImportedFiles { get; set; }
    public DbSet<TimetableYear> TimetableYears { get; set; }
    public DbSet<Train> Trains { get; set; }
    public DbSet<TrainTimetable> TrainTimetables { get; set; }
    public DbSet<TrainTimetableVariant> TrainTimetableVariants { get; set; }
    public DbSet<RoutingPoint> RoutingPoints { get; set; }
    public DbSet<CalendarDefinition> CalendarDefinitions { get; set; }
    public DbSet<NeighboringPoints> NeighboringPointTuples { get; set; }
    public DbSet<TrainCancellation> TrainCancellations { get; set; }
    public DbSet<Text> Texts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportedFile>()
            .HasIndex(o => o.FileName).IsUnique();
        modelBuilder.Entity<ImportedFile>()
            .HasIndex(o => o.CreationDate);

        modelBuilder.Entity<Train>()
            .HasIndex(o => o.Number).IsUnique();

        modelBuilder.Entity<TrainTimetable>()
            .HasIndex(o => new { o.TrainId, o.YearId }).IsUnique();
        modelBuilder.Entity<TrainTimetable>()
            .HasIndex(o => o.Name); // TODO: Fulltext

        modelBuilder.Entity<TrainTimetableVariant>()
            .HasIndex(o => new { o.YearId, o.TrainVariantId, o.PathVariantId }).IsUnique();

        modelBuilder.Entity<RoutingPoint>()
            .HasIndex(o => o.Code).IsUnique();
        modelBuilder.Entity<RoutingPoint>()
            .HasIndex(o => new { o.Latitude, o.Longitude }); // TODO: Geographic coordinates (R-Tree)
        modelBuilder.Entity<RoutingPoint>()
            .HasIndex(o => o.Name); // TODO: Fulltext

        modelBuilder.Entity<Passage>()
            .HasIndex(o => new { o.PointId, o.TrainId, o.Order }).IsUnique();
        modelBuilder.Entity<Passage>()
            .HasIndex(o => new { o.TrainId, o.Order }).IsUnique();
        modelBuilder.Entity<Passage>()
            .HasIndex(o => new { o.YearId, o.PointId, o.ArrivalTime });

        modelBuilder.Entity<NeighboringPoints>()
            .HasKey(o => new { o.PointAId, o.PointBId });
        modelBuilder.Entity<NeighboringPoints>()
            .HasIndex(o => new { o.PointBId, o.PointAId });

        modelBuilder.Entity<PttNoteForVariant>()
            .HasDiscriminator<int>("Kind")
            .HasValue<CentralPttNoteForVariant>(1)
            .HasValue<NonCentralPttNoteForVariant>(2);
        modelBuilder.Entity<PttNoteForVariant>()
            .HasIndex(o => o.TrainId);

        modelBuilder.Entity<TrainCancellation>()
            .HasIndex(o => o.TimetableVariantId);

        modelBuilder.Entity<Text>()
            .HasIndex(o => o.Str).IsUnique();
    }

    public HashSet<RoutingPoint> GetNeighboringPoints(RoutingPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        var pointId = point.Id;
        var neighbors = NeighboringPointTuples
            .Include(npt => npt.PointA)
            .Include(npt => npt.PointB)
            .Where(npt => npt.PointA.Id == pointId || npt.PointB.Id == pointId)
            .ToList();
        var result = new HashSet<RoutingPoint>(neighbors.Count);
        foreach (var neighbor in neighbors)
        {
            if (neighbor.PointA != point) result.Add(neighbor.PointA);
            else if (neighbor.PointB != point) result.Add(neighbor.PointB);
        }

        return result;
    }
}

public class ImportedFile
{
    public int Id { get; set; }

    [Required]
    public string FileName { get; set; }

    public long FileSize { get; set; }

    public DateTime ImportTime { get; set; }
    public DateTime CreationDate { get; set; }
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
    private static readonly Guid calendarNamespaceGuid = new("4eb0c41b-32a1-4acb-bd5f-8a6dbe847162");

    private bool[] bitmap;

    public int Id { get; set; }

    public int TimetableYearYear { get; set; }

    [Required]
    [ForeignKey("TimetableYearYear")]
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
        for (int i = 0; i < bits.Length; ++i, start = start.AddDays(1))
        {
            if (bits[i])
            {
                return ComputeGuid(start, bits, i);
            }
        }

        return Guid.Empty;
    }

    private static Guid ComputeGuid(DateTime startDate, bool[] bitmap, int startIndex)
    {
        return BuildGuidFromHash(String.Format(CultureInfo.InvariantCulture, "{0:o}|{1}", startDate,
            bitmap.Skip(startIndex).Aggregate(new StringBuilder(bitmap.Length - startIndex), (sb, value) =>
            {
                sb.Append(value ? '1' : '0');
                return sb;
            }).ToString()));
    }

    private static Guid BuildGuidFromHash(string data) => new(GuidEx.NewGuid(data, calendarNamespaceGuid).ToByteArray());

    public string DescriptionWithParens => StartDate > DateTime.Today ? "(" + Description + ")" : Description;
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

    // TODO: Point attributes
    [NotMapped]
    public PointType Type => Program.PointCodebook.Find(Code)?.Type ?? PointType.Unknown;

    [NotMapped]
    public String ShortName => Program.PointCodebook.Find(Code)?.ShortName ?? Name;

    [NotMapped]
    public string CountryCodeFromId => Code.Substring(0, Math.Max(Code.IndexOf(':'), 0));

    [NotMapped]
    public string ShortCzechIdentifier => Code.Substring(Math.Max(Code.IndexOf(':'), -1) + 1);

    [NotMapped]
    public string WikidataItem => Program.PointCodebook.Find(Code)?.WikidataItem;

    [InverseProperty("Point")]
    public List<Passage> PassingTrains { get; set; }
}

/**
  * Label of a train, common for all timetable years
  */
public class Train
{
    public int Id { get; set; }

    [MaxLength(20)]
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

    [InverseProperty("Timetable")]
    public virtual List<TrainTimetableVariant> Variants { get; set; }

    [NotMapped]
    public string TrainNumber => Train?.Number;

    public TrainCategory TrainCategory { get; set; }

    public TrafficType TrafficType { get; set; }
}

/**
  * Variant of a train in the respective timetable year
  */
public class TrainTimetableVariant
{
    public int Id { get; set; }

    [Required]
    public TrainTimetable Timetable { get; set; }

    // note that this is denormalized: TimetableId implies YearId, but it would be difficult to normalize it
    // and we need YearId to enforce unique index on Year+PathVariantId+TrainVariantId
    [Required]
    public int YearId { get; set; }

    [ForeignKey("YearId")]
    public TimetableYear TimetableYear { get; set; }

    [Required]
    [MaxLength(32)]
    public string PathVariantId { get; set; }

    [Required]
    [MaxLength(32)]
    public string TrainVariantId { get; set; }

    [Required]
    public CalendarDefinition Calendar { get; set; }

    public ImportedFile ImportedFrom { get; set; }

    public virtual List<Passage> Points { get; set; }

    public virtual List<PttNoteForVariant> PttNotes { get; set; }

    public virtual List<TrainCancellation> Cancellations { get; set; }
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

    [NotMapped]
    public bool IsMajorPoint => ArrivalTime != null || DwellTime != null;

    [NotMapped]
    public TimeSpan? AnyScheduledTime => ArrivalTime ?? DepartureTime;

    [NotMapped]
    public TimeSpan? ArrivalTimeOfDay => TimeOfDayOfTimeSpan(ArrivalTime);

    [NotMapped]
    public TimeSpan? DepartureTimeOfDay => TimeOfDayOfTimeSpan(DepartureTime);

    [NotMapped]
    public TimeSpan? AnyScheduledTimeOfDay => ArrivalTimeOfDay ?? DepartureTimeOfDay;

    // TODO: Point attributes
    [NotMapped]
    public string SubsidiaryLocationDescription => SubsidiaryLocation == null
        ? null
        : (DisplayConsts.SubsidiaryLocationTypeNames[SubsidiaryLocationType] + " " + SubsidiaryLocation + " " +
           SubsidiaryLocationName).Trim();

    [Column("TrainOperations")]
    public string TrainOperationsStr { get; set; }

    [NotMapped]
    public List<TrainOperation> TrainOperations
    {
        get => ParseEnumList<TrainOperation>(TrainOperationsStr);
        set => TrainOperationsStr = BuildEnumList(value);
    }

    public string SubsidiaryLocation { get; set; }

    public string SubsidiaryLocationName { get; set; }

    public SubsidiaryLocationType SubsidiaryLocationType { get; set; }

    private TimeSpan? TimeOfDayOfTimeSpan(TimeSpan? timeSpan)
    {
        if (timeSpan == null) return null;
        var value = timeSpan.GetValueOrDefault();
        if (value.Days == 0) return timeSpan;
        return new TimeSpan(0, value.Hours, value.Minutes, value.Seconds, value.Milliseconds);
    }

    public virtual List<NetworkSpecificParameterForPassage> NetworkSpecificParameters { get; set; }
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

public abstract class PttNoteForVariant
{
    public int Id { get; set; }

    [Required]
    public int TrainId { get; set; }

    [ForeignKey("TrainId")]
    public TrainTimetableVariant TrainTimetableVariant { get; set; }

    [Required]
    public Passage From { get; set; }

    [Required]
    public Passage To { get; set; }

    [Required]
    public bool OnArrival { get; set; }

    [Required]
    public CalendarDefinition Calendar { get; set; }
}

public class CentralPttNoteForVariant : PttNoteForVariant
{
    [Required]
    public CentralPttNote Type { get; set; }
}

public class NonCentralPttNoteForVariant : PttNoteForVariant
{
    [Required]
    public Text Text { get; set; }

    [Required]
    public HeaderDisplay ShowInHeader { get; set; }

    [Required]
    public FooterDisplay ShowInFooter { get; set; }

    [Required]
    public bool IsTariff { get; set; }
}

public class NetworkSpecificParameterForPassage
{
    public static readonly FrozenSet<NetworkSpecificParameterPassage> IndirectTypes = new[] { NetworkSpecificParameterPassage.CZPassengerPublicTransportOrderingCoName }.ToFrozenSet();

    public int Id { get; set; }

    [Required]
    public int PassageId { get; set; }

    [ForeignKey("PassageId")]
    public Passage Passage { get; set; }

    [Required]
    public NetworkSpecificParameterPassage Type { get; set; }

    [NotMapped]
    [Required]
    public string Value => ValueDirect ?? ValueIndirect?.Str;

    [Column("Value")]
    public string ValueDirect { get; set; }

    [ForeignKey("ValueRef")]
    public Text ValueIndirect { get; set; }
}

public class TrainCancellation
{
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string PathVariantId { get; set; }

    [Required]
    [MaxLength(32)]
    public string TrainVariantId { get; set; }

    public int? TimetableVariantId { get; set; }

    [ForeignKey("TimetableVariantId")]
    public TrainTimetableVariant TrainTimetableVariant { get; set; }

    [Required]
    public CalendarDefinition Calendar { get; set; }

    [Required]
    public ImportedFile ImportedFrom { get; set; }
}

public class Text
{
    public int Id { get; set; }

    [Required]
    public string Str { get; set; }

    public static Text FindOrCreate(DbModelContext context, string str)
    {
        var existing = context.Texts.SingleOrDefault(t => t.Str == str);
        if (existing != null) return existing;

        var newText = new Text { Str = str };
        context.Add(newText);
        context.SaveChanges();
        return newText;
    }
}

public static class DbModelUtils
{
    public static List<TEnum> ParseEnumList<TEnum>(string data)
        where TEnum : struct, Enum =>
        data == null
            ? []
            : data.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(Enum.Parse<TEnum>)
                .ToList();

    public static string BuildEnumList<TEnum>(List<TEnum> list)
        where TEnum : Enum =>
#pragma warning disable CA2021
        // ReSharper disable once SuspiciousTypeConversion.Global
        String.Join(';', list.Cast<int>());
#pragma warning restore CA2021
}