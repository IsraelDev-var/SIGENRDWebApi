using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGENRD.Infrastructure.Persistences.Migrations
{
    /// <inheritdoc />
    public partial class FlujoDeEstados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin es una columna de sistema de PostgreSQL: se usa como token de concurrencia sin crearla.

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "StateHistories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedKva",
                table: "ConnectionRequests",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "ReservedKva",
                table: "ConnectionRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "StateHistories",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
