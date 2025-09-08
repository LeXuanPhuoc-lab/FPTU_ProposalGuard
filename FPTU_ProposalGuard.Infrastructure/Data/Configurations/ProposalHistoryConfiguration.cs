using FPTU_ProposalGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ProposalGuard.Infrastructure.Data.Configurations;

public class ProposalHistoryConfiguration : IEntityTypeConfiguration<ProposalHistory>
{
    public void Configure(EntityTypeBuilder<ProposalHistory> builder)
    {
        builder.HasKey(e => e.HistoryId).HasName("PK_ProposalHistory_HistoryId");

        builder.ToTable("Proposal_History");

        builder.Property(e => e.HistoryId).HasColumnName("history_id");

        builder.Property(e => e.ProjectProposalId).HasColumnName("project_proposal_id");

        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .HasColumnName("status");
        builder.Property(e => e.MD5Hash)
            .HasMaxLength(255)
            .HasColumnName("md5_hash");

        builder.Property(e => e.Version).HasColumnName("version");
        builder.Property(e => e.ProposalCode)
            .HasMaxLength(50)
            .HasColumnName("proposal_code");

        builder.Property(e => e.Url)
            .HasMaxLength(255)
            .HasColumnName("url");

        builder.Property(e => e.Comment)
            .HasColumnName("comment");

        builder.Property(e => e.ProcessById).HasColumnName("process_by_id");

        builder.Property(e => e.ProcessDate)
            .HasColumnType("datetime")
            .HasColumnName("process_date");

        builder.HasOne(e => e.ProcessBy)
            .WithMany(u => u.ProposalHistories)
            .HasForeignKey(e => e.ProcessById)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_ProposalHistory_ProcessById");

        builder.HasOne(e => e.ProjectProposal)
            .WithMany(p => p.ProposalHistories)
            .HasForeignKey(e => e.ProjectProposalId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_ProposalHistory_ProjectProposalId");

        builder.HasMany(e => e.SimilarProposals)
            .WithOne(s => s.ProposalHistory)
            .HasForeignKey(s => s.HistoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ProposalSimilarity_HistoryId");
        
    }
}