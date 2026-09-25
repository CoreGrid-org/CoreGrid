using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetAnalysisToAgentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BudgetAnalysis",
                table: "AgentWorkflows",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetAnalysis",
                table: "AgentWorkflows");
        }
    }
}
