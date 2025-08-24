using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPTU_ProposalGuard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class remove_answer_field : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewAnswer_HistoryId",
                table: "Review_Answer");

            migrationBuilder.DropIndex(
                name: "IX_Review_Answer_history_id",
                table: "Review_Answer");

            migrationBuilder.DropColumn(
                name: "history_id",
                table: "Review_Answer");

            migrationBuilder.AddColumn<int>(
                name: "ProposalHistoryHistoryId",
                table: "Review_Answer",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Review_Answer_ProposalHistoryHistoryId",
                table: "Review_Answer",
                column: "ProposalHistoryHistoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Review_Answer_Proposal_History_ProposalHistoryHistoryId",
                table: "Review_Answer",
                column: "ProposalHistoryHistoryId",
                principalTable: "Proposal_History",
                principalColumn: "history_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Review_Answer_Proposal_History_ProposalHistoryHistoryId",
                table: "Review_Answer");

            migrationBuilder.DropIndex(
                name: "IX_Review_Answer_ProposalHistoryHistoryId",
                table: "Review_Answer");

            migrationBuilder.DropColumn(
                name: "ProposalHistoryHistoryId",
                table: "Review_Answer");

            migrationBuilder.AddColumn<int>(
                name: "history_id",
                table: "Review_Answer",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Review_Answer_history_id",
                table: "Review_Answer",
                column: "history_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewAnswer_HistoryId",
                table: "Review_Answer",
                column: "history_id",
                principalTable: "Proposal_History",
                principalColumn: "history_id");
        }
    }
}
