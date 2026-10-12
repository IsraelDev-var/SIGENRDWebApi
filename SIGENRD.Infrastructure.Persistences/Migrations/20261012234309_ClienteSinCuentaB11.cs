using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGENRD.Infrastructure.Persistences.Migrations
{
    /// <inheritdoc />
    public partial class ClienteSinCuentaB11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_ApplicationUserId",
                table: "customer");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "customer",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_customer_ApplicationUserId",
                table: "customer",
                column: "ApplicationUserId",
                unique: true,
                filter: "\"ApplicationUserId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_ApplicationUserId",
                table: "customer");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "customer",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_ApplicationUserId",
                table: "customer",
                column: "ApplicationUserId",
                unique: true);
        }
    }
}
