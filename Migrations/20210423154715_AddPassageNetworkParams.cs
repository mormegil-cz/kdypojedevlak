using Microsoft.EntityFrameworkCore.Migrations;

namespace KdyPojedeVlak.Migrations
{
    public partial class AddPassageNetworkParams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NetworkSpecificParameterForPassage",
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

            migrationBuilder.CreateIndex(
                name: "IX_NetworkSpecificParameterForPassage_PassageId",
                table: "NetworkSpecificParameterForPassage",
                column: "PassageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetworkSpecificParameterForPassage");
        }
    }
}
