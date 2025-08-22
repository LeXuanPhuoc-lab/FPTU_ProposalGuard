using FPTU_ProposalGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ProposalGuard.Infrastructure.Data.Configurations;

public class ReviewSessionConfiguration: IEntityTypeConfiguration<ReviewSession>
{
    public void Configure(EntityTypeBuilder<ReviewSession> builder)
    {
        builder.HasKey(s => s.SessionId).HasName("PK_ReviewSession_SessionId");

        builder.ToTable("Review_Session");

        builder.Property(s => s.SessionId).HasColumnName("session_id");

        builder.Property(s => s.HistoryId).HasColumnName("history_id");

        builder.Property(s => s.ReviewerId).HasColumnName("reviewer_id");

        builder.Property(s => s.ReviewStatus)
            .HasMaxLength(50)
            .HasColumnName("review_status");

        builder.Property(s => s.ReviewDate)
            .IsRequired(false)
            .HasColumnType("datetime")
            .HasColumnName("review_date");

        builder.HasOne(s => s.Reviewer)
            .WithMany(u => u.ReviewSessions)
            .HasForeignKey(s => s.ReviewerId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_ReviewSession_ReviewerId");

        builder.HasOne(s => s.History)
            .WithMany(h => h.ReviewSessions)
            .HasForeignKey(s => s.HistoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ReviewSession_HistoryId");
    }
}