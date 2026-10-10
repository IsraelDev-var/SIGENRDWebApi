using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGENRD.Infrastructure.Persistences.Migrations
{
    /// <inheritdoc />
    public partial class PerfilesUnicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InstallerCompanies_ApplicationUserId",
                table: "InstallerCompanies",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstallerCompanies_Rnc",
                table: "InstallerCompanies",
                column: "Rnc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_ApplicationUserId",
                table: "customer",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_NationalIdOrRnc",
                table: "customer",
                column: "NationalIdOrRnc",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InstallerCompanies_ApplicationUserId",
                table: "InstallerCompanies");

            migrationBuilder.DropIndex(
                name: "IX_InstallerCompanies_Rnc",
                table: "InstallerCompanies");

            migrationBuilder.DropIndex(
                name: "IX_customer_ApplicationUserId",
                table: "customer");

            migrationBuilder.DropIndex(
                name: "IX_customer_NationalIdOrRnc",
                table: "customer");
        }
    }
}
