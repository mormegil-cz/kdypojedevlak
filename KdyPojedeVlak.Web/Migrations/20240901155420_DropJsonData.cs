using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KdyPojedeVlak.Web.Migrations
{
    /// <inheritdoc />
    public partial class DropJsonData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrafficType",
                table: "TrainTimetables",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TrainCategory",
                table: "TrainTimetables",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                @"UPDATE TrainTimetables
	SET TrafficType = CASE json_extract(DataJson, '$.TrafficType')
        WHEN 'Os' THEN 1
        WHEN 'Ex' THEN 2
        WHEN 'R' THEN 3
        WHEN 'Sp' THEN 4
        WHEN 'Sv' THEN 5
        WHEN 'Nex' THEN 6
        WHEN 'Pn' THEN 7
        WHEN 'Mn' THEN 8
        WHEN 'Lv' THEN 9
        WHEN 'Vleč' THEN 10
        WHEN 'Služ' THEN 11
        WHEN 'Pom' THEN 12
		ELSE 0
	END,
	TrainCategory = CASE json_extract(DataJson, '$.TrainCategory')
		WHEN 'EuroCity' THEN 1
        WHEN 'Intercity' THEN 2
        WHEN 'Express' THEN 3
        WHEN 'EuroNight' THEN 4
        WHEN 'Regional' THEN 5
        WHEN 'SuperCity' THEN 6
        WHEN 'Rapid' THEN 7
        WHEN 'FastTrain' THEN 8
        WHEN 'RailJet' THEN 9
        WHEN 'Rex' THEN 10
        WHEN 'TrilexExpres' THEN 11
        WHEN 'Trilex' THEN 12
        WHEN 'LeoExpres' THEN 13
        WHEN 'Regiojet' THEN 14
        WHEN 'ArrivaExpress' THEN 15
        WHEN 'NightJet' THEN 16
        WHEN 'LeoExpresTenders' THEN 17
        WHEN 'EuroSleeper' THEN 18
		ELSE 0
	END;"
            );

            migrationBuilder.DropColumn(
                name: "DataJson",
                table: "TrainTimetables");

            migrationBuilder.DropColumn(
                name: "DataJson",
                table: "RoutingPoints");

            migrationBuilder.AddColumn<string>(
                name: "TrainOperations",
                table: "Passage",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubsidiaryLocation",
                table: "Passage",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubsidiaryLocationName",
                table: "Passage",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubsidiaryLocationType",
                table: "Passage",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                @"UPDATE Passage
	SET TrainOperations = json_extract(DataJson, '$.TrainOperations'),
	SubsidiaryLocation = json_extract(DataJson, '$.SubsidiaryLocation'),
	SubsidiaryLocationName = json_extract(DataJson, '$.SubsidiaryLocationName'),
	SubsidiaryLocationType = CASE json_extract(DataJson, '$.SubsidiaryLocationType')
	WHEN '' THEN NULL
	WHEN 'None' THEN 1
	WHEN 'StationTrack' THEN 2
	ELSE 0 END"
            );

            migrationBuilder.DropColumn(
                name: "DataJson",
                table: "Passage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataJson",
                table: "TrainTimetables",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                                 UPDATE TrainTimetables
                                     SET DataJson = CONCAT('{"TrafficType":"', CASE TrafficType
                                         WHEN 1 THEN 'Os' 
                                         WHEN 2 THEN 'Ex' 
                                         WHEN 3 THEN 'R' 
                                         WHEN 4 THEN 'Sp' 
                                         WHEN 5 THEN 'Sv' 
                                         WHEN 6 THEN 'Nex' 
                                         WHEN 7 THEN 'Pn' 
                                         WHEN 8 THEN 'Mn' 
                                         WHEN 9 THEN 'Lv' 
                                         WHEN 10 THEN 'Vleč' 
                                         WHEN 11 THEN 'Služ' 
                                         WHEN 12 THEN 'Pom' 
                                 		ELSE 'Unknown'
                                 	END, '", "TrainCategory":"', CASE TrainCategory
                                 		WHEN 1 THEN 'EuroCity' 
                                         WHEN 2 THEN 'Intercity' 
                                         WHEN 3 THEN 'Express' 
                                         WHEN 4 THEN 'EuroNight' 
                                         WHEN 5 THEN 'Regional' 
                                         WHEN 6 THEN 'SuperCity' 
                                         WHEN 7 THEN 'Rapid' 
                                         WHEN 8 THEN 'FastTrain' 
                                         WHEN 9 THEN 'RailJet' 
                                         WHEN 10 THEN 'Rex' 
                                         WHEN 11 THEN 'TrilexExpres' 
                                         WHEN 12 THEN 'Trilex' 
                                         WHEN 13 THEN 'LeoExpres' 
                                         WHEN 14 THEN 'Regiojet' 
                                         WHEN 15 THEN 'ArrivaExpress' 
                                         WHEN 16 THEN 'NightJet' 
                                         WHEN 17 THEN 'LeoExpresTenders' 
                                         WHEN 18 THEN 'EuroSleeper' 
                                 		ELSE 'Unknown'
                                 	END, '"}')
                                 """);
            
            migrationBuilder.DropColumn(
                name: "TrafficType",
                table: "TrainTimetables");

            migrationBuilder.Sql("""
                                 UPDATE Passage
                                    SET DataJson=CONCAT('{"TrainOperations":"', TrainOperations,
                                                        '","SubsidiaryLocation":"', SubsidiaryLocation,
                                                        '","SubsidiaryLocationName":"', SubsidiaryLocationName,
                                                        '","SubsidiaryLocationType":"', SubsidiaryLocationType, '"}')
                                 """);
            
            migrationBuilder.DropColumn(
                name: "TrainCategory",
                table: "TrainTimetables");

            migrationBuilder.DropColumn(
                name: "SubsidiaryLocation",
                table: "Passage");

            migrationBuilder.DropColumn(
                name: "SubsidiaryLocationName",
                table: "Passage");

            migrationBuilder.DropColumn(
                name: "SubsidiaryLocationType",
                table: "Passage");

            migrationBuilder.AddColumn<string>(
                name: "DataJson",
                table: "RoutingPoints",
                type: "TEXT",
                nullable: true);
        }
    }
}
