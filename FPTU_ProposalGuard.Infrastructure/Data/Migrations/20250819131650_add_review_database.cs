using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPTU_ProposalGuard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_review_database : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Review_Session",
                columns: table => new
                {
                    session_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    reviewer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    history_id = table.Column<int>(type: "int", nullable: false),
                    review_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    review_status = table.Column<int>(type: "int", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewSession_SessionId", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_ReviewSession_HistoryId",
                        column: x => x.history_id,
                        principalTable: "Proposal_History",
                        principalColumn: "history_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewSession_ReviewerId",
                        column: x => x.reviewer_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "ReviewQuestion",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    question_content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    answer_type = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewQuestion", x => x.QuestionId);
                });

            migrationBuilder.CreateTable(
                name: "Review_Answer",
                columns: table => new
                {
                    answer_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    review_session_id = table.Column<int>(type: "int", nullable: false),
                    question_id = table.Column<int>(type: "int", nullable: false),
                    history_id = table.Column<int>(type: "int", nullable: false),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewAnswer_AnswerId", x => x.answer_id);
                    table.ForeignKey(
                        name: "FK_ReviewAnswer_HistoryId",
                        column: x => x.history_id,
                        principalTable: "Proposal_History",
                        principalColumn: "history_id");
                    table.ForeignKey(
                        name: "FK_ReviewAnswer_QuestionId",
                        column: x => x.question_id,
                        principalTable: "ReviewQuestion",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewAnswer_SessionId",
                        column: x => x.review_session_id,
                        principalTable: "Review_Session",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Review_Answer_history_id",
                table: "Review_Answer",
                column: "history_id");

            migrationBuilder.CreateIndex(
                name: "IX_Review_Answer_question_id",
                table: "Review_Answer",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_Review_Answer_review_session_id",
                table: "Review_Answer",
                column: "review_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_Review_Session_history_id",
                table: "Review_Session",
                column: "history_id");

            migrationBuilder.CreateIndex(
                name: "IX_Review_Session_reviewer_id",
                table: "Review_Session",
                column: "reviewer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Review_Answer");

            migrationBuilder.DropTable(
                name: "ReviewQuestion");

            migrationBuilder.DropTable(
                name: "Review_Session");
        }
    }
}
