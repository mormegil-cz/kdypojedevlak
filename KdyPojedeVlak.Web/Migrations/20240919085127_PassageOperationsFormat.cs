using System;
using System.Linq;
using System.Text;
using KdyPojedeVlak.Web.Engine.Djr;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KdyPojedeVlak.Web.Migrations
{
    /// <inheritdoc />
    public partial class PassageOperationsFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = new StringBuilder("UPDATE Passage SET TrainOperations=");
            var ops = Enum.GetValues<TrainOperation>();
            foreach (var op in ops) sql.Append("replace(");
            sql.Append("TrainOperations");
            foreach (var op in ops)
            {
                sql.Append(", '");
                sql.Append(op);
                sql.Append("', '");
                sql.Append((int) op);
                sql.Append("')");
            }
            sql.Append(" where TrainOperations!=''");
            migrationBuilder.Sql(sql.ToString());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = new StringBuilder("UPDATE Passage SET TrainOperations=");
            var ops = Enum.GetValues<TrainOperation>();
            foreach (var op in ops) sql.Append("replace(");
            sql.Append("TrainOperations");
            foreach (var op in ops.Reverse())
            {
                sql.Append(", '");
                sql.Append((int) op);
                sql.Append("', '");
                sql.Append(op);
                sql.Append("')");
            }
            sql.Append(" where TrainOperations!=''");
            migrationBuilder.Sql(sql.ToString());
        }
    }
}
