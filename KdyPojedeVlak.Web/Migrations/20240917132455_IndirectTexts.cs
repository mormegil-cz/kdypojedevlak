using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KdyPojedeVlak.Web.Migrations
{
    /// <inheritdoc />
    public partial class IndirectTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Texts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Str = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Texts", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO Texts(Str)
                    SELECT DISTINCT Value
                    FROM NetworkSpecificParameterForPassage
                    WHERE Type=4
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_Texts_Str",
                table: "Texts",
                column: "Str",
                unique: true);

            migrationBuilder.DropIndex(
                "IX_NetworkSpecificParameterForPassage_PassageId",
                "NetworkSpecificParameterForPassage"
            );

            migrationBuilder.CreateTable(
                name: "TmpNetworkSpecificParameterForPassage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PassageId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    ValueRef = table.Column<int>(type: "INTEGER", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkSpecificParameterForPassage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkSpecificParameterForPassage_Passage_PassageId",
                        column: x => x.PassageId,
                        principalTable: "Passage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NetworkSpecificParameterForPassage_Texts_ValueRef",
                        column: x => x.ValueRef,
                        principalTable: "Texts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.CheckConstraint("CHK_NetworkSpecificParameterForPassage_Ref_ValueRef", "Value IS NULL OR ValueRef IS NULL");
                });

            migrationBuilder.Sql("""
                                 INSERT INTO TmpNetworkSpecificParameterForPassage(PassageId, Type, Value, ValueRef)
                                 SELECT PassageId, Type, iif(Type=4, NULL, Value), iif(Type=4, (SELECT t.ID FROM Texts t WHERE t.Str=Value), NULL)
                                 FROM NetworkSpecificParameterForPassage
                                 """);

            migrationBuilder.DropTable("NetworkSpecificParameterForPassage");

            migrationBuilder.RenameTable("TmpNetworkSpecificParameterForPassage", null, "NetworkSpecificParameterForPassage", null);

            migrationBuilder.CreateIndex(
                name: "IX_NetworkSpecificParameterForPassage_PassageId",
                table: "NetworkSpecificParameterForPassage",
                column: "PassageId");

            
            migrationBuilder.Sql("""
                                 INSERT OR IGNORE INTO Texts(Str) SELECT DISTINCT Text FROM PttNoteForVariant WHERE Kind=2
                                 """);

            migrationBuilder.DropIndex(
                name: "IX_PttNoteForVariant_CalendarId",
                table: "PttNoteForVariant");

            migrationBuilder.DropIndex(
                name: "IX_PttNoteForVariant_FromId",
                table: "PttNoteForVariant");

            migrationBuilder.DropIndex(
                name: "IX_PttNoteForVariant_ToId",
                table: "PttNoteForVariant");

            migrationBuilder.DropIndex(
                name: "IX_PttNoteForVariant_TrainId",
                table: "PttNoteForVariant");


            migrationBuilder.CreateTable(
                name: "TmpPttNoteForVariant",
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
                    TextId = table.Column<int>(type: "INTEGER", nullable: true),
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
                        principalTable: "TrainTimetableVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PttNoteForVariant_Texts_Text",
                        column: x => x.TextId,
                        principalTable: "Texts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                                 INSERT INTO TmpPttNoteForVariant(CalendarId, FromId, IsTariff, Kind, OnArrival, ShowInFooter, ShowInHeader, TextId, ToId, TrainId, Type)
                                    SELECT CalendarId, FromId, IsTariff, Kind, OnArrival, ShowInFooter, ShowInHeader, iif(Kind=2, (SELECT Id FROM Texts t WHERE t.Str=Text), NULL), ToId, TrainId, Type
                                    FROM PttNoteForVariant
                                 """);

            migrationBuilder.DropTable("PttNoteForVariant");

            migrationBuilder.RenameTable("TmpPttNoteForVariant", null, "PttNoteForVariant", null);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PttNoteForVariant_CalendarId",
                table: "PttNoteForVariant");

            migrationBuilder.DropIndex(
                name: "IX_PttNoteForVariant_FromId",
                table: "PttNoteForVariant");

            migrationBuilder.DropIndex(
                name: "IX_PttNoteForVariant_ToId",
                table: "PttNoteForVariant");

            migrationBuilder.DropIndex(
                name: "IX_PttNoteForVariant_TrainId",
                table: "PttNoteForVariant");

            migrationBuilder.CreateTable(
                name: "TmpPttNoteForVariant",
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
                        principalTable: "TrainTimetableVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                                 INSERT INTO TmpPttNoteForVariant(CalendarId, FromId, IsTariff, Kind, OnArrival, ShowInFooter, ShowInHeader, Text, ToId, TrainId, Type)
                                    SELECT CalendarId, FromId, IsTariff, Kind, OnArrival, ShowInFooter, ShowInHeader, iif(Kind=2, (SELECT Str FROM Texts t WHERE t.Id=TextId), NULL), ToId, TrainId, Type
                                    FROM PttNoteForVariant
                                 """);

            migrationBuilder.DropTable("PttNoteForVariant");

            migrationBuilder.RenameTable("TmpPttNoteForVariant", null, "PttNoteForVariant", null);

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
            

            migrationBuilder.DropIndex(
                "IX_NetworkSpecificParameterForPassage_PassageId",
                "NetworkSpecificParameterForPassage"
            );
            
            migrationBuilder.CreateTable(
                name: "TmpNetworkSpecificParameterForPassage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PassageId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkSpecificParameterForPassage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkSpecificParameterForPassage_Passage_PassageId",
                        column: x => x.PassageId,
                        principalTable: "Passage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                                 INSERT INTO TmpNetworkSpecificParameterForPassage(PassageId, Type, Value)
                                 SELECT PassageId, Type, iif(ValueRef IS NOT NULL, (SELECT t.Str FROM Texts t WHERE t.ID=ValueRef), Value)
                                 FROM NetworkSpecificParameterForPassage
                                 """);

            migrationBuilder.DropTable("NetworkSpecificParameterForPassage");

            migrationBuilder.RenameTable("TmpNetworkSpecificParameterForPassage", null, "NetworkSpecificParameterForPassage", null);

            migrationBuilder.CreateIndex(
                name: "IX_NetworkSpecificParameterForPassage_PassageId",
                table: "NetworkSpecificParameterForPassage",
                column: "PassageId");
            
            
            migrationBuilder.DropTable(
                name: "Texts");
        }
    }
}
