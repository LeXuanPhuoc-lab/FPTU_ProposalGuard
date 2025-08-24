using FPTU_ProposalGuard.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ProposalGuard.Infrastructure.Data.Configurations;

public class ReviewAnswerConfiguration : IEntityTypeConfiguration<ReviewAnswer>
{
    public void Configure(EntityTypeBuilder<ReviewAnswer> builder)
    {
        builder.HasKey(a => a.AnswerId)
            .HasName("PK_ReviewAnswer_AnswerId");

        builder.ToTable("Review_Answer");

        builder.Property(a => a.AnswerId)
            .HasColumnName("answer_id");

        builder.Property(a => a.ReviewSessionId)
            .HasColumnName("review_session_id");

        builder.Property(a => a.QuestionId)
            .HasColumnName("question_id");
        
        builder.Property(a => a.Answer)
            .HasColumnName("answer");

        builder.HasOne(a => a.ReviewSession)
            .WithMany(s => s.Answers)
            .HasForeignKey(a => a.ReviewSessionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ReviewAnswer_SessionId");

        builder.HasOne(a => a.Question)
            .WithMany(q => q.ReviewAnswers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.ClientSetNull) 
            .HasConstraintName("FK_ReviewAnswer_QuestionId");
    }
}