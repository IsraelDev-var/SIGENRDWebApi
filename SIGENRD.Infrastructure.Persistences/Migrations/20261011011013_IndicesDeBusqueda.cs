using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGENRD.Infrastructure.Persistences.Migrations
{
    /// <inheritdoc />
    public partial class IndicesDeBusqueda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_ProjectAddress_trgm",
                table: "ConnectionRequests",
                column: "ProjectAddress")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_RequestCode_trgm",
                table: "ConnectionRequests",
                column: "RequestCode")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_RequestedAt",
                table: "ConnectionRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_Status_RequestedAt",
                table: "ConnectionRequests",
                columns: new[] { "Status", "RequestedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConnectionRequests_ProjectAddress_trgm",
                table: "ConnectionRequests");

            migrationBuilder.DropIndex(
                name: "IX_ConnectionRequests_RequestCode_trgm",
                table: "ConnectionRequests");

            migrationBuilder.DropIndex(
                name: "IX_ConnectionRequests_RequestedAt",
                table: "ConnectionRequests");

            migrationBuilder.DropIndex(
                name: "IX_ConnectionRequests_Status_RequestedAt",
                table: "ConnectionRequests");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
