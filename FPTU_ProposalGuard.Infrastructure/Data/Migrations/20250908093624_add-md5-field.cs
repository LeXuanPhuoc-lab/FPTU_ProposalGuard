using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPTU_ProposalGuard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addmd5field : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "md5_hash",
                table: "Proposal_History",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "md5_hash",
                table: "Proposal_History");
        }
    }
}
