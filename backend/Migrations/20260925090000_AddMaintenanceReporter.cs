using System;
using CoreGrid.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreGrid.Api.Migrations;

[DbContext(typeof(CoreGridDbContext))]
[Migration("20260925090000_AddMaintenanceReporter")]
public partial class AddMaintenanceReporter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ReportedByUserId",
            table: "MaintenanceRecords",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_MaintenanceRecords_ReportedByUserId",
            table: "MaintenanceRecords",
            column: "ReportedByUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_MaintenanceRecords_Users_ReportedByUserId",
            table: "MaintenanceRecords",
            column: "ReportedByUserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_MaintenanceRecords_Users_ReportedByUserId", table: "MaintenanceRecords");
        migrationBuilder.DropIndex(name: "IX_MaintenanceRecords_ReportedByUserId", table: "MaintenanceRecords");
        migrationBuilder.DropColumn(name: "ReportedByUserId", table: "MaintenanceRecords");
    }
}
