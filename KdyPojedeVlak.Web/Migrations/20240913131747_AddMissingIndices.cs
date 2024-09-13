using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KdyPojedeVlak.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_NeighboringPointTuples_PointAId_PointBId",
                table: "NeighboringPointTuples",
                columns: new[] { "PointAId", "PointBId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
