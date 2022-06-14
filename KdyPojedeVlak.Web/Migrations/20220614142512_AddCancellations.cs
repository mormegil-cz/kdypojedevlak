using Microsoft.EntityFrameworkCore.Migrations;

namespace KdyPojedeVlak.Web.Migrations
{
    public partial class AddCancellations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Passage_TrainTimetableVariant_TrainId",
                table: "Passage");

            migrationBuilder.DropForeignKey(
                name: "FK_PttNoteForVariant_TrainTimetableVariant_TrainId",
                table: "PttNoteForVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainTimetableVariant_CalendarDefinitions_CalendarId",
                table: "TrainTimetableVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainTimetableVariant_ImportedFiles_ImportedFromId",
                table: "TrainTimetableVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainTimetableVariant_TrainTimetables_TimetableId",
                table: "TrainTimetableVariant");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainTimetableVariant",
                table: "TrainTimetableVariant");

            migrationBuilder.RenameTable(
                name: "TrainTimetableVariant",
                newName: "TrainTimetableVariants");

            migrationBuilder.RenameIndex(
                name: "IX_TrainTimetableVariant_TimetableId",
                table: "TrainTimetableVariants",
                newName: "IX_TrainTimetableVariants_TimetableId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainTimetableVariant_ImportedFromId",
                table: "TrainTimetableVariants",
                newName: "IX_TrainTimetableVariants_ImportedFromId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainTimetableVariant_CalendarId",
                table: "TrainTimetableVariants",
                newName: "IX_TrainTimetableVariants_CalendarId");

            migrationBuilder.AddColumn<int>(
                name: "YearId",
                table: "TrainTimetableVariants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"UPDATE TrainTimetableVariants SET YearId=TrainTimetables.YearId FROM TrainTimetables WHERE TrainTimetableVariants.TimetableId=TrainTimetables.Id");
            
            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainTimetableVariants",
                table: "TrainTimetableVariants",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "TrainCancellations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PathVariantId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TrainVariantId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TimetableVariantId = table.Column<int>(type: "INTEGER", nullable: true),
                    CalendarId = table.Column<int>(type: "INTEGER", nullable: true),
                    ImportedFromId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainCancellations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainCancellations_CalendarDefinitions_CalendarId",
                        column: x => x.CalendarId,
                        principalTable: "CalendarDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainCancellations_ImportedFiles_ImportedFromId",
                        column: x => x.ImportedFromId,
                        principalTable: "ImportedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainCancellations_TrainTimetableVariants_TimetableVariantId",
                        column: x => x.TimetableVariantId,
                        principalTable: "TrainTimetableVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainTimetableVariants_YearId_TrainVariantId_PathVariantId",
                table: "TrainTimetableVariants",
                columns: new[] { "YearId", "TrainVariantId", "PathVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainCancellations_CalendarId",
                table: "TrainCancellations",
                column: "CalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainCancellations_ImportedFromId",
                table: "TrainCancellations",
                column: "ImportedFromId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainCancellations_TimetableVariantId",
                table: "TrainCancellations",
                column: "TimetableVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Passage_TrainTimetableVariants_TrainId",
                table: "Passage",
                column: "TrainId",
                principalTable: "TrainTimetableVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PttNoteForVariant_TrainTimetableVariants_TrainId",
                table: "PttNoteForVariant",
                column: "TrainId",
                principalTable: "TrainTimetableVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainTimetableVariants_CalendarDefinitions_CalendarId",
                table: "TrainTimetableVariants",
                column: "CalendarId",
                principalTable: "CalendarDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainTimetableVariants_ImportedFiles_ImportedFromId",
                table: "TrainTimetableVariants",
                column: "ImportedFromId",
                principalTable: "ImportedFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainTimetableVariants_TimetableYears_YearId",
                table: "TrainTimetableVariants",
                column: "YearId",
                principalTable: "TimetableYears",
                principalColumn: "Year",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainTimetableVariants_TrainTimetables_TimetableId",
                table: "TrainTimetableVariants",
                column: "TimetableId",
                principalTable: "TrainTimetables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Passage_TrainTimetableVariants_TrainId",
                table: "Passage");

            migrationBuilder.DropForeignKey(
                name: "FK_PttNoteForVariant_TrainTimetableVariants_TrainId",
                table: "PttNoteForVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainTimetableVariants_CalendarDefinitions_CalendarId",
                table: "TrainTimetableVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainTimetableVariants_ImportedFiles_ImportedFromId",
                table: "TrainTimetableVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainTimetableVariants_TimetableYears_YearId",
                table: "TrainTimetableVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainTimetableVariants_TrainTimetables_TimetableId",
                table: "TrainTimetableVariants");

            migrationBuilder.DropTable(
                name: "TrainCancellations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainTimetableVariants",
                table: "TrainTimetableVariants");

            migrationBuilder.DropIndex(
                name: "IX_TrainTimetableVariants_YearId_TrainVariantId_PathVariantId",
                table: "TrainTimetableVariants");

            migrationBuilder.DropColumn(
                name: "YearId",
                table: "TrainTimetableVariants");

            migrationBuilder.RenameTable(
                name: "TrainTimetableVariants",
                newName: "TrainTimetableVariant");

            migrationBuilder.RenameIndex(
                name: "IX_TrainTimetableVariants_TimetableId",
                table: "TrainTimetableVariant",
                newName: "IX_TrainTimetableVariant_TimetableId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainTimetableVariants_ImportedFromId",
                table: "TrainTimetableVariant",
                newName: "IX_TrainTimetableVariant_ImportedFromId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainTimetableVariants_CalendarId",
                table: "TrainTimetableVariant",
                newName: "IX_TrainTimetableVariant_CalendarId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainTimetableVariant",
                table: "TrainTimetableVariant",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Passage_TrainTimetableVariant_TrainId",
                table: "Passage",
                column: "TrainId",
                principalTable: "TrainTimetableVariant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PttNoteForVariant_TrainTimetableVariant_TrainId",
                table: "PttNoteForVariant",
                column: "TrainId",
                principalTable: "TrainTimetableVariant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainTimetableVariant_CalendarDefinitions_CalendarId",
                table: "TrainTimetableVariant",
                column: "CalendarId",
                principalTable: "CalendarDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainTimetableVariant_ImportedFiles_ImportedFromId",
                table: "TrainTimetableVariant",
                column: "ImportedFromId",
                principalTable: "ImportedFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainTimetableVariant_TrainTimetables_TimetableId",
                table: "TrainTimetableVariant",
                column: "TimetableId",
                principalTable: "TrainTimetables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
