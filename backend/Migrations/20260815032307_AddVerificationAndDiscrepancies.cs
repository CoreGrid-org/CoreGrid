using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationAndDiscrepancies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerificationCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    ScopeDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScopeLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScopeAssetCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScopeAssetTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationCampaigns_AssetCategories_ScopeAssetCategoryId",
                        column: x => x.ScopeAssetCategoryId,
                        principalTable: "AssetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationCampaigns_AssetTypes_ScopeAssetTypeId",
                        column: x => x.ScopeAssetTypeId,
                        principalTable: "AssetTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationCampaigns_Departments_ScopeDepartmentId",
                        column: x => x.ScopeDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationCampaigns_Locations_ScopeLocationId",
                        column: x => x.ScopeLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationCampaigns_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationCampaigns_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VerificationTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssertedPresent = table.Column<bool>(type: "boolean", nullable: true),
                    AssertedLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssertedCondition = table.Column<string>(type: "text", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationTasks_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationTasks_Locations_AssertedLocationId",
                        column: x => x.AssertedLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationTasks_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationTasks_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationTasks_Users_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationTasks_VerificationCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "VerificationCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Discrepancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false),
                    RaisedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResolutionType = table.Column<string>(type: "text", nullable: true),
                    ResolutionExplanation = table.Column<string>(type: "text", nullable: true),
                    CorrectiveAction = table.Column<string>(type: "text", nullable: true),
                    RegisterCorrected = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discrepancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Discrepancies_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Discrepancies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Discrepancies_Users_RaisedByUserId",
                        column: x => x.RaisedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Discrepancies_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Discrepancies_VerificationCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "VerificationCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Discrepancies_VerificationTasks_VerificationTaskId",
                        column: x => x.VerificationTaskId,
                        principalTable: "VerificationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_AssetId",
                table: "Discrepancies",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_CampaignId",
                table: "Discrepancies",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_OrganizationId",
                table: "Discrepancies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_RaisedByUserId",
                table: "Discrepancies",
                column: "RaisedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_ResolvedByUserId",
                table: "Discrepancies",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_Status",
                table: "Discrepancies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_Type",
                table: "Discrepancies",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_VerificationTaskId",
                table: "Discrepancies",
                column: "VerificationTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCampaigns_CreatedByUserId",
                table: "VerificationCampaigns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCampaigns_OrganizationId",
                table: "VerificationCampaigns",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCampaigns_ScopeAssetCategoryId",
                table: "VerificationCampaigns",
                column: "ScopeAssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCampaigns_ScopeAssetTypeId",
                table: "VerificationCampaigns",
                column: "ScopeAssetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCampaigns_ScopeDepartmentId",
                table: "VerificationCampaigns",
                column: "ScopeDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCampaigns_ScopeLocationId",
                table: "VerificationCampaigns",
                column: "ScopeLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCampaigns_Status",
                table: "VerificationCampaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationTasks_AssertedLocationId",
                table: "VerificationTasks",
                column: "AssertedLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationTasks_AssetId",
                table: "VerificationTasks",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationTasks_AssignedToUserId",
                table: "VerificationTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationTasks_CampaignId",
                table: "VerificationTasks",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationTasks_CompletedByUserId",
                table: "VerificationTasks",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationTasks_OrganizationId",
                table: "VerificationTasks",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationTasks_Status",
                table: "VerificationTasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Discrepancies");

            migrationBuilder.DropTable(
                name: "VerificationTasks");

            migrationBuilder.DropTable(
                name: "VerificationCampaigns");
        }
    }
}
