using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Objective = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Plan = table.Column<string>(type: "jsonb", nullable: true),
                    AgentOutputs = table.Column<string>(type: "jsonb", nullable: true),
                    ToolCalls = table.Column<string>(type: "jsonb", nullable: true),
                    ValidationResult = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendation = table.Column<string>(type: "text", nullable: true),
                    IsHighImpact = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    RevisionCount = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    InitiatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkflows", x => x.Id);
                    table.CheckConstraint("CK_AgentWorkflows_Recommendation", "\"Recommendation\" IS NULL OR \"Recommendation\" IN ('REPAIR','REPLACE','TRANSFER','DISPOSE','RETAIN')");
                    table.ForeignKey(
                        name: "FK_AgentWorkflows_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentWorkflows_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentWorkflows_Users_InitiatedByUserId",
                        column: x => x.InitiatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    WorkflowSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentApprovals", x => x.Id);
                    table.CheckConstraint("CK_AgentApprovals_Decision", "\"Decision\" IN ('APPROVE','REJECT','REVISE')");
                    table.ForeignKey(
                        name: "FK_AgentApprovals_AgentWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "AgentWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentApprovals_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentExecutionSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Agent = table.Column<string>(type: "text", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    InputHash = table.Column<string>(type: "text", nullable: true),
                    OutputSummary = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentExecutionSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentExecutionSteps_AgentWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "AgentWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentApprovals_DecidedByUserId",
                table: "AgentApprovals",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentApprovals_WorkflowId",
                table: "AgentApprovals",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutionSteps_WorkflowId",
                table: "AgentExecutionSteps",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_AssetId",
                table: "AgentWorkflows",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_CorrelationId",
                table: "AgentWorkflows",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_InitiatedByUserId",
                table: "AgentWorkflows",
                column: "InitiatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_OrganizationId",
                table: "AgentWorkflows",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_Status",
                table: "AgentWorkflows",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentApprovals");

            migrationBuilder.DropTable(
                name: "AgentExecutionSteps");

            migrationBuilder.DropTable(
                name: "AgentWorkflows");
        }
    }
}
