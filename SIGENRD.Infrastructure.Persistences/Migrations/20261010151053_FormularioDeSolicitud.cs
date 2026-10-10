using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGENRD.Infrastructure.Persistences.Migrations
{
    /// <inheritdoc />
    public partial class FormularioDeSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificationStandard",
                table: "Generators",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertifyingUnit",
                table: "Generators",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratorKind",
                table: "Generators",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "InverterBased");

            migrationBuilder.AddColumn<int>(
                name: "Phases",
                table: "Generators",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Generators",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLegalEntity",
                table: "customer",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeclarationAcceptedAt",
                table: "ConnectionRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineerCodiaNumber",
                table: "ConnectionRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineerCompany",
                table: "ConnectionRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineerEmail",
                table: "ConnectionRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineerFullName",
                table: "ConnectionRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineerPhone",
                table: "ConnectionRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeterNumber",
                table: "ConnectionRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerFullName",
                table: "ConnectionRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerNationalId",
                table: "ConnectionRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerPhone",
                table: "ConnectionRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropertyRelation",
                table: "ConnectionRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Owner");

            migrationBuilder.AddColumn<string>(
                name: "ServiceContractNumber",
                table: "ConnectionRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificationStandard",
                table: "Generators");

            migrationBuilder.DropColumn(
                name: "CertifyingUnit",
                table: "Generators");

            migrationBuilder.DropColumn(
                name: "GeneratorKind",
                table: "Generators");

            migrationBuilder.DropColumn(
                name: "Phases",
                table: "Generators");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Generators");

            migrationBuilder.DropColumn(
                name: "IsLegalEntity",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "DeclarationAcceptedAt",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "EngineerCodiaNumber",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "EngineerCompany",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "EngineerEmail",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "EngineerFullName",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "EngineerPhone",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "MeterNumber",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "OwnerFullName",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "OwnerNationalId",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "OwnerPhone",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "PropertyRelation",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "ServiceContractNumber",
                table: "ConnectionRequests");
        }
    }
}
