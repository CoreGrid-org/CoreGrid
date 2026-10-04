using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgentWorkflowAssetTypeScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "AssetId",
                table: "AgentWorkflows",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AssetTypeId",
                table: "AgentWorkflows",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Every existing workflow targeted one asset — its type becomes the scope.
            migrationBuilder.Sql(
                "UPDATE \"AgentWorkflows\" w SET \"AssetTypeId\" = a.\"AssetTypeId\" " +
                "FROM \"Assets\" a WHERE a.\"Id\" = w.\"AssetId\";");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_AssetTypeId",
                table: "AgentWorkflows",
                column: "AssetTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentWorkflows_AssetTypes_AssetTypeId",
                table: "AgentWorkflows",
                column: "AssetTypeId",
                principalTable: "AssetTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentWorkflows_AssetTypes_AssetTypeId",
                table: "AgentWorkflows");

            migrationBuilder.DropIndex(
                name: "IX_AgentWorkflows_AssetTypeId",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "AssetTypeId",
                table: "AgentWorkflows");

            // Asset-type-wide workflows have no single asset to fall back to.
            migrationBuilder.Sql("DELETE FROM \"AgentWorkflows\" WHERE \"AssetId\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "AssetId",
                table: "AgentWorkflows",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
