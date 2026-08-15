using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferDisposalForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DisposalRequests_AssetId",
                table: "DisposalRequests",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransfers_AssetId",
                table: "AssetTransfers",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransfers_FromDepartmentId",
                table: "AssetTransfers",
                column: "FromDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransfers_FromLocationId",
                table: "AssetTransfers",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransfers_ToDepartmentId",
                table: "AssetTransfers",
                column: "ToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransfers_ToLocationId",
                table: "AssetTransfers",
                column: "ToLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTransfers_Assets_AssetId",
                table: "AssetTransfers",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTransfers_Departments_FromDepartmentId",
                table: "AssetTransfers",
                column: "FromDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTransfers_Departments_ToDepartmentId",
                table: "AssetTransfers",
                column: "ToDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTransfers_Locations_FromLocationId",
                table: "AssetTransfers",
                column: "FromLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTransfers_Locations_ToLocationId",
                table: "AssetTransfers",
                column: "ToLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DisposalRequests_Assets_AssetId",
                table: "DisposalRequests",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetTransfers_Assets_AssetId",
                table: "AssetTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_AssetTransfers_Departments_FromDepartmentId",
                table: "AssetTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_AssetTransfers_Departments_ToDepartmentId",
                table: "AssetTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_AssetTransfers_Locations_FromLocationId",
                table: "AssetTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_AssetTransfers_Locations_ToLocationId",
                table: "AssetTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_DisposalRequests_Assets_AssetId",
                table: "DisposalRequests");

            migrationBuilder.DropIndex(
                name: "IX_DisposalRequests_AssetId",
                table: "DisposalRequests");

            migrationBuilder.DropIndex(
                name: "IX_AssetTransfers_AssetId",
                table: "AssetTransfers");

            migrationBuilder.DropIndex(
                name: "IX_AssetTransfers_FromDepartmentId",
                table: "AssetTransfers");

            migrationBuilder.DropIndex(
                name: "IX_AssetTransfers_FromLocationId",
                table: "AssetTransfers");

            migrationBuilder.DropIndex(
                name: "IX_AssetTransfers_ToDepartmentId",
                table: "AssetTransfers");

            migrationBuilder.DropIndex(
                name: "IX_AssetTransfers_ToLocationId",
                table: "AssetTransfers");
        }
    }
}
