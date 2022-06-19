using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KdyPojedeVlak.Web.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // initial database is created automatically
            return;
            #pragma warning disable CS0162

            migrationBuilder.CreateTable(
                name: "ImportedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    ImportTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutingPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Latitude = table.Column<float>(type: "REAL", nullable: true),
                    Longitude = table.Column<float>(type: "REAL", nullable: true),
                    DataJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimetableYears",
                columns: table => new
                {
                    Year = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MinDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaxDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimetableYears", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "Trains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NeighboringPointTuples",
                columns: table => new
                {
                    PointAId = table.Column<int>(type: "INTEGER", nullable: false),
                    PointBId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NeighboringPointTuples", x => new { x.PointAId, x.PointBId });
                    table.ForeignKey(
                        name: "FK_NeighboringPointTuples_RoutingPoints_PointAId",
                        column: x => x.PointAId,
                        principalTable: "RoutingPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NeighboringPointTuples_RoutingPoints_PointBId",
                        column: x => x.PointBId,
                        principalTable: "RoutingPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimetableYearYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Guid = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BitmapEncoded = table.Column<string>(type: "TEXT", maxLength: 70, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarDefinitions_TimetableYears_TimetableYearYear",
                        column: x => x.TimetableYearYear,
                        principalTable: "TimetableYears",
                        principalColumn: "Year",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainTimetables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrainId = table.Column<int>(type: "INTEGER", nullable: false),
                    YearId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DataJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainTimetables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainTimetables_TimetableYears_YearId",
                        column: x => x.YearId,
                        principalTable: "TimetableYears",
                        principalColumn: "Year",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainTimetables_Trains_TrainId",
                        column: x => x.TrainId,
                        principalTable: "Trains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainTimetableVariant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimetableId = table.Column<int>(type: "INTEGER", nullable: false),
                    PathVariantId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TrainVariantId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CalendarId = table.Column<int>(type: "INTEGER", nullable: true),
                    DataJson = table.Column<string>(type: "TEXT", nullable: true),
                    ImportedFromId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainTimetableVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainTimetableVariant_CalendarDefinitions_CalendarId",
                        column: x => x.CalendarId,
                        principalTable: "CalendarDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainTimetableVariant_ImportedFiles_ImportedFromId",
                        column: x => x.ImportedFromId,
                        principalTable: "ImportedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainTimetableVariant_TrainTimetables_TimetableId",
                        column: x => x.TimetableId,
                        principalTable: "TrainTimetables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Passage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    YearId = table.Column<int>(type: "INTEGER", nullable: false),
                    PointId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    ArrivalTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    DepartureTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    DwellTime = table.Column<decimal>(type: "TEXT", nullable: true),
                    ArrivalDay = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartureDay = table.Column<int>(type: "INTEGER", nullable: false),
                    DataJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Passage_RoutingPoints_PointId",
                        column: x => x.PointId,
                        principalTable: "RoutingPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Passage_TimetableYears_YearId",
                        column: x => x.YearId,
                        principalTable: "TimetableYears",
                        principalColumn: "Year",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Passage_TrainTimetableVariant_TrainId",
                        column: x => x.TrainId,
                        principalTable: "TrainTimetableVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDefinitions_TimetableYearYear",
                table: "CalendarDefinitions",
                column: "TimetableYearYear");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedFiles_FileName",
                table: "ImportedFiles",
                column: "FileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NeighboringPointTuples_PointBId_PointAId",
                table: "NeighboringPointTuples",
                columns: new[] { "PointBId", "PointAId" });

            migrationBuilder.CreateIndex(
                name: "IX_Passage_PointId_TrainId_Order",
                table: "Passage",
                columns: new[] { "PointId", "TrainId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Passage_TrainId_Order",
                table: "Passage",
                columns: new[] { "TrainId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Passage_YearId_PointId_ArrivalTime",
                table: "Passage",
                columns: new[] { "YearId", "PointId", "ArrivalTime" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutingPoints_Code",
                table: "RoutingPoints",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutingPoints_Latitude_Longitude",
                table: "RoutingPoints",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutingPoints_Name",
                table: "RoutingPoints",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Trains_Number",
                table: "Trains",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainTimetables_Name",
                table: "TrainTimetables",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TrainTimetables_TrainId_YearId",
                table: "TrainTimetables",
                columns: new[] { "TrainId", "YearId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainTimetables_YearId",
                table: "TrainTimetables",
                column: "YearId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainTimetableVariant_CalendarId",
                table: "TrainTimetableVariant",
                column: "CalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainTimetableVariant_ImportedFromId",
                table: "TrainTimetableVariant",
                column: "ImportedFromId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainTimetableVariant_TimetableId",
                table: "TrainTimetableVariant",
                column: "TimetableId");
#pragma warning restore CS0162
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NeighboringPointTuples");

            migrationBuilder.DropTable(
                name: "Passage");

            migrationBuilder.DropTable(
                name: "RoutingPoints");

            migrationBuilder.DropTable(
                name: "TrainTimetableVariant");

            migrationBuilder.DropTable(
                name: "CalendarDefinitions");

            migrationBuilder.DropTable(
                name: "ImportedFiles");

            migrationBuilder.DropTable(
                name: "TrainTimetables");

            migrationBuilder.DropTable(
                name: "TimetableYears");

            migrationBuilder.DropTable(
                name: "Trains");
        }
    }
}
