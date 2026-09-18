using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameMaintenancePhotoUrlToPhotoObjectKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhotoUrl",
                table: "MaintenanceRecords",
                newName: "PhotoObjectKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhotoObjectKey",
                table: "MaintenanceRecords",
                newName: "PhotoUrl");
        }
    }
}
