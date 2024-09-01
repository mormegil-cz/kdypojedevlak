﻿// <auto-generated />
using System;
using KdyPojedeVlak.Web.Engine.DbStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace KdyPojedeVlak.Web.Migrations
{
    [DbContext(typeof(DbModelContext))]
    [Migration("20240901161554_VacuumAfterDroppingJsonData")]
    partial class VacuumAfterDroppingJsonData
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.CalendarDefinition", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("BitmapEncoded")
                        .IsRequired()
                        .HasMaxLength(70)
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Guid")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("TimetableYearYear")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("TimetableYearYear");

                    b.ToTable("CalendarDefinitions");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.ImportedFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("FileSize")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ImportTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CreationDate");

                    b.HasIndex("FileName")
                        .IsUnique();

                    b.ToTable("ImportedFiles");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.NeighboringPoints", b =>
                {
                    b.Property<int>("PointAId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PointBId")
                        .HasColumnType("INTEGER");

                    b.HasKey("PointAId", "PointBId");

                    b.HasIndex("PointBId", "PointAId");

                    b.ToTable("NeighboringPointTuples");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.NetworkSpecificParameterForPassage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("PassageId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("PassageId");

                    b.ToTable("NetworkSpecificParameterForPassage");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.Passage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ArrivalDay")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan?>("ArrivalTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("DepartureDay")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan?>("DepartureTime")
                        .HasColumnType("TEXT");

                    b.Property<decimal?>("DwellTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PointId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SubsidiaryLocation")
                        .HasColumnType("TEXT");

                    b.Property<string>("SubsidiaryLocationName")
                        .HasColumnType("TEXT");

                    b.Property<int>("SubsidiaryLocationType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TrainId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TrainOperationsStr")
                        .HasColumnType("TEXT")
                        .HasColumnName("TrainOperations");

                    b.Property<int>("YearId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("TrainId", "Order")
                        .IsUnique();

                    b.HasIndex("PointId", "TrainId", "Order")
                        .IsUnique();

                    b.HasIndex("YearId", "PointId", "ArrivalTime");

                    b.ToTable("Passage");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.PttNoteForVariant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CalendarId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("FromId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Kind")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("OnArrival")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ToId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TrainId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("CalendarId");

                    b.HasIndex("FromId");

                    b.HasIndex("ToId");

                    b.HasIndex("TrainId");

                    b.ToTable("PttNoteForVariant");

                    b.HasDiscriminator<int>("Kind");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.RoutingPoint", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("TEXT");

                    b.Property<float?>("Latitude")
                        .HasColumnType("REAL");

                    b.Property<float?>("Longitude")
                        .HasColumnType("REAL");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("Name");

                    b.HasIndex("Latitude", "Longitude");

                    b.ToTable("RoutingPoints");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.TimetableYear", b =>
                {
                    b.Property<int>("Year")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("MaxDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("MinDate")
                        .HasColumnType("TEXT");

                    b.HasKey("Year");

                    b.ToTable("TimetableYears");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.Train", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Number")
                        .HasMaxLength(20)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Number")
                        .IsUnique();

                    b.ToTable("Trains");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.TrainCancellation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CalendarId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ImportedFromId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PathVariantId")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<int?>("TimetableVariantId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TrainVariantId")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CalendarId");

                    b.HasIndex("ImportedFromId");

                    b.HasIndex("TimetableVariantId");

                    b.ToTable("TrainCancellations");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.TrainTimetable", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<int>("TrafficType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TrainCategory")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TrainId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("YearId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.HasIndex("YearId");

                    b.HasIndex("TrainId", "YearId")
                        .IsUnique();

                    b.ToTable("TrainTimetables");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.TrainTimetableVariant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CalendarId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ImportedFromId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PathVariantId")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<int>("TimetableId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TrainVariantId")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<int>("YearId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("CalendarId");

                    b.HasIndex("ImportedFromId");

                    b.HasIndex("TimetableId");

                    b.HasIndex("YearId", "TrainVariantId", "PathVariantId")
                        .IsUnique();

                    b.ToTable("TrainTimetableVariants");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.CentralPttNoteForVariant", b =>
                {
                    b.HasBaseType("KdyPojedeVlak.Web.Engine.DbStorage.PttNoteForVariant");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasDiscriminator().HasValue(1);
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.NonCentralPttNoteForVariant", b =>
                {
                    b.HasBaseType("KdyPojedeVlak.Web.Engine.DbStorage.PttNoteForVariant");

                    b.Property<bool>("IsTariff")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ShowInFooter")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ShowInHeader")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasDiscriminator().HasValue(2);
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.CalendarDefinition", b =>
                {
                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.TimetableYear", "TimetableYear")
                        .WithMany()
                        .HasForeignKey("TimetableYearYear")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TimetableYear");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.NeighboringPoints", b =>
                {
                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.RoutingPoint", "PointA")
                        .WithMany()
                        .HasForeignKey("PointAId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.RoutingPoint", "PointB")
                        .WithMany()
                        .HasForeignKey("PointBId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PointA");

                    b.Navigation("PointB");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.NetworkSpecificParameterForPassage", b =>
                {
                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.Passage", "Passage")
                        .WithMany("NetworkSpecificParameters")
                        .HasForeignKey("PassageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Passage");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.Passage", b =>
                {
                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.RoutingPoint", "Point")
                        .WithMany("PassingTrains")
                        .HasForeignKey("PointId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.TrainTimetableVariant", "TrainTimetableVariant")
                        .WithMany("Points")
                        .HasForeignKey("TrainId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.TimetableYear", "Year")
                        .WithMany()
                        .HasForeignKey("YearId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Point");

                    b.Navigation("TrainTimetableVariant");

                    b.Navigation("Year");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.PttNoteForVariant", b =>
                {
                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.CalendarDefinition", "Calendar")
                        .WithMany()
                        .HasForeignKey("CalendarId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.Passage", "From")
                        .WithMany()
                        .HasForeignKey("FromId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.Passage", "To")
                        .WithMany()
                        .HasForeignKey("ToId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.TrainTimetableVariant", "TrainTimetableVariant")
                        .WithMany("PttNotes")
                        .HasForeignKey("TrainId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Calendar");

                    b.Navigation("From");

                    b.Navigation("To");

                    b.Navigation("TrainTimetableVariant");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.TrainCancellation", b =>
                {
                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.CalendarDefinition", "Calendar")
                        .WithMany()
                        .HasForeignKey("CalendarId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.ImportedFile", "ImportedFrom")
                        .WithMany()
                        .HasForeignKey("ImportedFromId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.TrainTimetableVariant", "TrainTimetableVariant")
                        .WithMany("Cancellations")
                        .HasForeignKey("TimetableVariantId");

                    b.Navigation("Calendar");

                    b.Navigation("ImportedFrom");

                    b.Navigation("TrainTimetableVariant");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.TrainTimetable", b =>
                {
                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.Train", "Train")
                        .WithMany()
                        .HasForeignKey("TrainId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.TimetableYear", "TimetableYear")
                        .WithMany()
                        .HasForeignKey("YearId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TimetableYear");

                    b.Navigation("Train");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.TrainTimetableVariant", b =>
                {
                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.CalendarDefinition", "Calendar")
                        .WithMany()
                        .HasForeignKey("CalendarId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.ImportedFile", "ImportedFrom")
                        .WithMany()
                        .HasForeignKey("ImportedFromId");

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.TrainTimetable", "Timetable")
                        .WithMany("Variants")
                        .HasForeignKey("TimetableId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KdyPojedeVlak.Web.Engine.DbStorage.TimetableYear", "TimetableYear")
                        .WithMany()
                        .HasForeignKey("YearId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Calendar");

                    b.Navigation("ImportedFrom");

                    b.Navigation("Timetable");

                    b.Navigation("TimetableYear");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.Passage", b =>
                {
                    b.Navigation("NetworkSpecificParameters");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.RoutingPoint", b =>
                {
                    b.Navigation("PassingTrains");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.TrainTimetable", b =>
                {
                    b.Navigation("Variants");
                });

            modelBuilder.Entity("KdyPojedeVlak.Web.Engine.DbStorage.TrainTimetableVariant", b =>
                {
                    b.Navigation("Cancellations");

                    b.Navigation("Points");

                    b.Navigation("PttNotes");
                });
#pragma warning restore 612, 618
        }
    }
}
