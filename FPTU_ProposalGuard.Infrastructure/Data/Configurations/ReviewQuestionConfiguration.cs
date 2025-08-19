using FPTU_ProposalGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ProposalGuard.Infrastructure.Data.Configurations;

public class ReviewQuestionConfiguration : IEntityTypeConfiguration<ReviewQuestion>
{
    public void Configure(EntityTypeBuilder<ReviewQuestion> builder)
    {
        builder.HasKey(q => q.QuestionId);

        builder.ToTable("ReviewQuestion");

        builder.Property(q => q.QuestionContent)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("question_content");

        builder.Property(q => q.AnswerType)
            .HasConversion<string>()
            .HasColumnName("answer_type");
        
        builder.HasMany(q => q.ReviewAnswers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ReviewAnswer_QuestionId");
    }
}