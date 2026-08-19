using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ObservedCondition = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    WorkPerformed = table.Column<string>(type: "text", nullable: true),
                    CompletionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ResultingCondition = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceRecords_ObservedCondition", "\"ObservedCondition\" IN ('NEW','GOOD','FAIR','POOR','UNSERVICEABLE')");
                    table.CheckConstraint("CK_MaintenanceRecords_Priority", "\"Priority\" IN ('LOW','MEDIUM','HIGH','CRITICAL')");
                    table.CheckConstraint("CK_MaintenanceRecords_ResultingCondition", "\"ResultingCondition\" IS NULL OR \"ResultingCondition\" IN ('NEW','GOOD','FAIR','POOR','UNSERVICEABLE')");
                    table.CheckConstraint("CK_MaintenanceRecords_Status", "\"Status\" IN ('REQUESTED','APPROVED','IN_PROGRESS','COMPLETED','CANCELLED')");
                    table.CheckConstraint("CK_MaintenanceRecords_Type", "\"Type\" IN ('CORRECTIVE','PREVENTIVE')");
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_AssetId",
                table: "MaintenanceRecords",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_AssigneeId",
                table: "MaintenanceRecords",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_OrganizationId",
                table: "MaintenanceRecords",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_Priority",
                table: "MaintenanceRecords",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_Status",
                table: "MaintenanceRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_Type",
                table: "MaintenanceRecords",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceRecords");
        }
    }
}
