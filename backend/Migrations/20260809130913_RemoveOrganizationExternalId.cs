using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrganizationExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_ExternalOrgId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ExternalOrgId",
                table: "Organizations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalOrgId",
                table: "Organizations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_ExternalOrgId",
                table: "Organizations",
                column: "ExternalOrgId",
                unique: true);
        }
    }
}
