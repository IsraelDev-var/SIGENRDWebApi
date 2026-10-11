using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SIGENRD.Infrastructure.Persistences.Migrations
{
    /// <inheritdoc />
    public partial class SimulacionMedicionNeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NetMeteringSimulations_UserId",
                table: "NetMeteringSimulations");

            migrationBuilder.AlterColumn<decimal>(
                name: "Roi",
                table: "NetMeteringSimulations",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PaybackYears",
                table: "NetMeteringSimulations",
                type: "numeric(8,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualBillAfter",
                table: "NetMeteringSimulations",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualBillBefore",
                table: "NetMeteringSimulations",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualExportCreditKwh",
                table: "NetMeteringSimulations",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Assumptions",
                table: "NetMeteringSimulations",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<decimal>(
                name: "CoveragePct",
                table: "NetMeteringSimulations",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FlatPricePerKwh",
                table: "NetMeteringSimulations",
                type: "numeric(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormulaVersion",
                table: "NetMeteringSimulations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "NetMeteringSimulations",
                type: "geography (point, 4326)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyConsumptionKwh",
                table: "NetMeteringSimulations",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "NetMeteringSimulations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Recommendations",
                table: "NetMeteringSimulations",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.AddColumn<decimal>(
                name: "SurplusPayment",
                table: "NetMeteringSimulations",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "TransformerDistanceMeters",
                table: "NetMeteringSimulations",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NetMeteringSimulations_UserId_CreatedAt",
                table: "NetMeteringSimulations",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NetMeteringSimulations_UserId_CreatedAt",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "AnnualBillAfter",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "AnnualBillBefore",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "AnnualExportCreditKwh",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "Assumptions",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "CoveragePct",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "FlatPricePerKwh",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "FormulaVersion",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "MonthlyConsumptionKwh",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "Recommendations",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "SurplusPayment",
                table: "NetMeteringSimulations");

            migrationBuilder.DropColumn(
                name: "TransformerDistanceMeters",
                table: "NetMeteringSimulations");

            migrationBuilder.AlterColumn<decimal>(
                name: "Roi",
                table: "NetMeteringSimulations",
                type: "numeric(8,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PaybackYears",
                table: "NetMeteringSimulations",
                type: "numeric(8,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,2)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NetMeteringSimulations_UserId",
                table: "NetMeteringSimulations",
                column: "UserId");
        }
    }
}
