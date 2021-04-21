using Microsoft.EntityFrameworkCore.Migrations;

namespace KdyPojedeVlak.Migrations
{
    public partial class AddPttNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataJson",
                table: "TrainTimetableVariant");

            migrationBuilder.CreateTable(
                name: "PttNoteForVariant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrainId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromId = table.Column<int>(type: "INTEGER", nullable: true),
                    ToId = table.Column<int>(type: "INTEGER", nullable: true),
                    OnArrival = table.Column<bool>(type: "INTEGER", nullable: false),
                    CalendarId = table.Column<int>(type: "INTEGER", nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    ShowInHeader = table.Column<int>(type: "INTEGER", nullable: true),
                    ShowInFooter = table.Column<int>(type: "INTEGER", nullable: true),
                    IsTariff = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PttNoteForVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PttNoteForVariant_CalendarDefinitions_CalendarId",
                        column: x => x.CalendarId,
                        principalTable: "CalendarDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PttNoteForVariant_Passage_FromId",
                        column: x => x.FromId,
                        principalTable: "Passage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PttNoteForVariant_Passage_ToId",
                        column: x => x.ToId,
                        principalTable: "Passage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PttNoteForVariant_TrainTimetableVariant_TrainId",
                        column: x => x.TrainId,
                        principalTable: "TrainTimetableVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PttNoteForVariant_CalendarId",
                table: "PttNoteForVariant",
                column: "CalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_PttNoteForVariant_FromId",
                table: "PttNoteForVariant",
                column: "FromId");

            migrationBuilder.CreateIndex(
                name: "IX_PttNoteForVariant_ToId",
                table: "PttNoteForVariant",
                column: "ToId");

            migrationBuilder.CreateIndex(
                name: "IX_PttNoteForVariant_TrainId",
                table: "PttNoteForVariant",
                column: "TrainId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PttNoteForVariant");

            migrationBuilder.AddColumn<string>(
                name: "DataJson",
                table: "TrainTimetableVariant",
                type: "TEXT",
                nullable: true);
        }
    }
}
