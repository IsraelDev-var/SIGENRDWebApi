using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGENRD.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class PersonalPorEde : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistributorId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            // Hasta ahora solo existía Edesur (Id 1 en la base de negocio): el personal que ya había queda en ella.
            // La base de negocio es otra, así que no se puede comprobar aquí; el administrador puede reasignarlo.
            migrationBuilder.Sql(
                """UPDATE "AspNetUsers" SET "DistributorId" = 1 WHERE "UserType" IN (2, 4) AND "DistributorId" IS NULL""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistributorId",
                table: "AspNetUsers");
        }
    }
}
