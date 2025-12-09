using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIGENRD.Infrastructure.Persistences.Migrations
{
    /// <inheritdoc />
    public partial class InitialBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NationalIdOrRnc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Distributors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distributors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstallerCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TradeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Rnc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallerCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ZoneName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Municipality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Engineers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    InstallerCompanyId = table.Column<int>(type: "integer", nullable: false),
                    CodiaNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Specialty = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsVerifiedByCodia = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Engineers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Engineers_InstallerCompanies_InstallerCompanyId",
                        column: x => x.InstallerCompanyId,
                        principalTable: "InstallerCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transformers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DistributorId = table.Column<int>(type: "integer", nullable: false),
                    ServiceZoneId = table.Column<int>(type: "integer", nullable: false),
                    TotalCapacityKva = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    AvailableCapacityKva = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", maxLength: 50, nullable: false, defaultValue: 1),
                    Location = table.Column<Point>(type: "geography (Point, 4326)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transformers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transformers_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transformers_ServiceZones_ServiceZoneId",
                        column: x => x.ServiceZoneId,
                        principalTable: "ServiceZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    InstallerCompanyId = table.Column<int>(type: "integer", nullable: false),
                    DistributorId = table.Column<int>(type: "integer", nullable: false),
                    TransformerId = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UsageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TariffType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InterconnectionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProjectDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProjectAddress = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Coordinates = table.Column<Point>(type: "geography (Point, 4326)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionRequests_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionRequests_InstallerCompanies_InstallerCompanyId",
                        column: x => x.InstallerCompanyId,
                        principalTable: "InstallerCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionRequests_Transformers_TransformerId",
                        column: x => x.TransformerId,
                        principalTable: "Transformers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionRequests_customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetMeteringRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectionRequestId = table.Column<int>(type: "integer", nullable: false),
                    GenerationSystemType = table.Column<int>(type: "integer", nullable: false),
                    InstalledCapacityKw = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    ApprovalCertificateUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetMeteringRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetMeteringRequests_ConnectionRequests_ConnectionRequestId",
                        column: x => x.ConnectionRequestId,
                        principalTable: "ConnectionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewObservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectionRequestId = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewObservations_ConnectionRequests_ConnectionRequestId",
                        column: x => x.ConnectionRequestId,
                        principalTable: "ConnectionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectionRequestId = table.Column<int>(type: "integer", nullable: false),
                    PreviousState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NewState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Comment = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateHistories_ConnectionRequests_ConnectionRequestId",
                        column: x => x.ConnectionRequestId,
                        principalTable: "ConnectionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectionRequestId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalDocuments_ConnectionRequests_ConnectionRequestId",
                        column: x => x.ConnectionRequestId,
                        principalTable: "ConnectionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Generators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NetMeteringRequestId = table.Column<int>(type: "integer", nullable: false),
                    GenerationSystemType = table.Column<int>(type: "integer", nullable: false),
                    InverterModel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RatedPowerKva = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ConnectionVoltage = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    NominalCurrent = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PowerFactorMin = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    PowerFactorMax = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SwitchingType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HarmonicDistortionPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Generators_NetMeteringRequests_NetMeteringRequestId",
                        column: x => x.NetMeteringRequestId,
                        principalTable: "NetMeteringRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_CustomerId",
                table: "ConnectionRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_DistributorId",
                table: "ConnectionRequests",
                column: "DistributorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_InstallerCompanyId",
                table: "ConnectionRequests",
                column: "InstallerCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_TransformerId",
                table: "ConnectionRequests",
                column: "TransformerId");

            migrationBuilder.CreateIndex(
                name: "IX_Engineers_InstallerCompanyId",
                table: "Engineers",
                column: "InstallerCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Generators_NetMeteringRequestId",
                table: "Generators",
                column: "NetMeteringRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_NetMeteringRequests_ConnectionRequestId",
                table: "NetMeteringRequests",
                column: "ConnectionRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewObservations_ConnectionRequestId",
                table: "ReviewObservations",
                column: "ConnectionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_StateHistories_ConnectionRequestId",
                table: "StateHistories",
                column: "ConnectionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDocuments_ConnectionRequestId",
                table: "TechnicalDocuments",
                column: "ConnectionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Transformers_DistributorId",
                table: "Transformers",
                column: "DistributorId");

            migrationBuilder.CreateIndex(
                name: "IX_Transformers_ServiceZoneId",
                table: "Transformers",
                column: "ServiceZoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Engineers");

            migrationBuilder.DropTable(
                name: "Generators");

            migrationBuilder.DropTable(
                name: "ReviewObservations");

            migrationBuilder.DropTable(
                name: "StateHistories");

            migrationBuilder.DropTable(
                name: "TechnicalDocuments");

            migrationBuilder.DropTable(
                name: "NetMeteringRequests");

            migrationBuilder.DropTable(
                name: "ConnectionRequests");

            migrationBuilder.DropTable(
                name: "InstallerCompanies");

            migrationBuilder.DropTable(
                name: "Transformers");

            migrationBuilder.DropTable(
                name: "customer");

            migrationBuilder.DropTable(
                name: "Distributors");

            migrationBuilder.DropTable(
                name: "ServiceZones");
        }
    }
}
