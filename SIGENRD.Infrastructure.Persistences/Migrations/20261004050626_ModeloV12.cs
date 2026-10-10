using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIGENRD.Infrastructure.Persistences.Migrations
{
    /// <inheritdoc />
    public partial class ModeloV12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StateHistories_ConnectionRequests_ConnectionRequestId",
                table: "StateHistories");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "NetMeteringRequests");

            migrationBuilder.RenameColumn(
                name: "DocumentType",
                table: "ReviewObservations",
                newName: "ObservationType");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Transformers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Available",
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 50,
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedCapacityKva",
                table: "Transformers",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "TechnicalDocuments",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TechnicalDocuments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "TechnicalDocuments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "TechnicalDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "StateHistories",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "StateHistories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "InstallerCompanies",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationStatus",
                table: "InstallerCompanies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<decimal>(
                name: "AvailableThresholdPct",
                table: "Distributors",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 70m);

            migrationBuilder.AddColumn<decimal>(
                name: "ConditionedThresholdPct",
                table: "Distributors",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 30m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPowerFactor",
                table: "Distributors",
                type: "numeric(4,3)",
                nullable: false,
                defaultValue: 0.9m);

            migrationBuilder.AlterColumn<int>(
                name: "InstallerCompanyId",
                table: "ConnectionRequests",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "RequestType",
                table: "ConnectionRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedCapacityKva",
                table: "ConnectionRequests",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetMeteringSimulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DistributorId = table.Column<int>(type: "integer", nullable: true),
                    TransformerId = table.Column<int>(type: "integer", nullable: true),
                    InstalledCapacityKw = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    AnnualProductionKwh = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    AnnualSavings = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    InvestmentCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PaybackYears = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Roi = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    ViabilityStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetMeteringSimulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetMeteringSimulations_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NetMeteringSimulations_Transformers_TransformerId",
                        column: x => x.TransformerId,
                        principalTable: "Transformers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SimulationGenerators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NetMeteringSimulationId = table.Column<int>(type: "integer", nullable: false),
                    GenerationSystemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PanelModel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    InverterModel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RatedPowerKw = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationGenerators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationGenerators_NetMeteringSimulations_NetMeteringSimu~",
                        column: x => x.NetMeteringSimulationId,
                        principalTable: "NetMeteringSimulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transformers_Code",
                table: "Transformers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transformers_Location",
                table: "Transformers",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_RequestCode",
                table: "ConnectionRequests",
                column: "RequestCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_NetMeteringSimulations_DistributorId",
                table: "NetMeteringSimulations",
                column: "DistributorId");

            migrationBuilder.CreateIndex(
                name: "IX_NetMeteringSimulations_TransformerId",
                table: "NetMeteringSimulations",
                column: "TransformerId");

            migrationBuilder.CreateIndex(
                name: "IX_NetMeteringSimulations_UserId",
                table: "NetMeteringSimulations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationGenerators_NetMeteringSimulationId",
                table: "SimulationGenerators",
                column: "NetMeteringSimulationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StateHistories_ConnectionRequests_ConnectionRequestId",
                table: "StateHistories",
                column: "ConnectionRequestId",
                principalTable: "ConnectionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // --- Ajuste de datos existentes por el cambio de enums ---
            // Transformers.Status pasó de entero a texto: 1 Available, 2 Conditioned, 3 Saturated, 4 Maintenance (valores anteriores).
            migrationBuilder.Sql(@"UPDATE ""Transformers"" SET ""Status"" = CASE ""Status""
                WHEN '1' THEN 'Available' WHEN '2' THEN 'Conditioned' WHEN '3' THEN 'Saturated' WHEN '4' THEN 'Maintenance'
                ELSE ""Status"" END;");
            // El estado Completed ya no existe en la Fase I.
            migrationBuilder.Sql(@"UPDATE ""ConnectionRequests"" SET ""Status"" = 'Approved' WHERE ""Status"" = 'Completed';");

            // --- RN-21: el historial y la auditoría solo admiten inserción, incluso para el usuario de la aplicación ---
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION prevent_modification() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'La tabla % es de solo inserción (RN-21).', TG_TABLE_NAME;
                END;
                $$ LANGUAGE plpgsql;");
            migrationBuilder.Sql(@"CREATE TRIGGER trg_state_histories_immutable BEFORE UPDATE OR DELETE ON ""StateHistories"" FOR EACH ROW EXECUTE FUNCTION prevent_modification();");
            migrationBuilder.Sql(@"CREATE TRIGGER trg_audit_logs_immutable BEFORE UPDATE OR DELETE ON ""AuditLogs"" FOR EACH ROW EXECUTE FUNCTION prevent_modification();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_audit_logs_immutable ON ""AuditLogs"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_state_histories_immutable ON ""StateHistories"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS prevent_modification();");

            migrationBuilder.DropForeignKey(
                name: "FK_StateHistories_ConnectionRequests_ConnectionRequestId",
                table: "StateHistories");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "SimulationGenerators");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "NetMeteringSimulations");

            migrationBuilder.DropIndex(
                name: "IX_Transformers_Code",
                table: "Transformers");

            migrationBuilder.DropIndex(
                name: "IX_Transformers_Location",
                table: "Transformers");

            migrationBuilder.DropIndex(
                name: "IX_ConnectionRequests_RequestCode",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "ReservedCapacityKva",
                table: "Transformers");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "TechnicalDocuments");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "TechnicalDocuments");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "InstallerCompanies");

            migrationBuilder.DropColumn(
                name: "ValidationStatus",
                table: "InstallerCompanies");

            migrationBuilder.DropColumn(
                name: "AvailableThresholdPct",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "ConditionedThresholdPct",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "DefaultPowerFactor",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "RequestType",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "RequestedCapacityKva",
                table: "ConnectionRequests");

            migrationBuilder.RenameColumn(
                name: "ObservationType",
                table: "ReviewObservations",
                newName: "DocumentType");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Transformers",
                type: "integer",
                maxLength: 50,
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Available");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "TechnicalDocuments",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TechnicalDocuments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "StateHistories",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "StateHistories",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "NetMeteringRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "InstallerCompanyId",
                table: "ConnectionRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StateHistories_ConnectionRequests_ConnectionRequestId",
                table: "StateHistories",
                column: "ConnectionRequestId",
                principalTable: "ConnectionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
