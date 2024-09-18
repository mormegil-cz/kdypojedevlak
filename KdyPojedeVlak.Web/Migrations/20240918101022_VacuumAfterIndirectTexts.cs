using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KdyPojedeVlak.Web.Migrations
{
    /// <inheritdoc />
    public partial class VacuumAfterIndirectTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("VACUUM", true);
            migrationBuilder.Sql("PRAGMA optimize", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
