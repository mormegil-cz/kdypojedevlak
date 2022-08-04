using Microsoft.EntityFrameworkCore.Migrations;

namespace KdyPojedeVlak.Web.Migrations
{
    public partial class AddNewestTrainsIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ImportedFiles_CreationDate",
                table: "ImportedFiles",
                column: "CreationDate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportedFiles_CreationDate",
                table: "ImportedFiles");
        }
    }
}
