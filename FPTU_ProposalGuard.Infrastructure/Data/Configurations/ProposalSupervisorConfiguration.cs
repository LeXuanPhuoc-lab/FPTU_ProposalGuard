﻿using FPTU_ProposalGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ProposalGuard.Infrastructure.Data.Configurations;

public class ProposalSupervisorConfiguration : IEntityTypeConfiguration<ProposalSupervisor>
{
    public void Configure(EntityTypeBuilder<ProposalSupervisor> builder)
    {
        builder.HasKey(e => e.ProposalSupervisorId).HasName("PK_ProposalSupervisor_ProposalSupervisorId");

        builder.ToTable("Proposal_Supervisor");

        builder.Property(e => e.ProposalSupervisorId)
            .ValueGeneratedOnAdd()
            .HasColumnName("proposal_supervisor_id");
        builder.Property(e => e.ProjectProposalId)
            .HasColumnName("project_proposal_id");
        builder.Property(e => e.Email)
            .HasMaxLength(255)
            .HasColumnName("email");
        builder.Property(e => e.FullName)
            .HasMaxLength(255)
            .HasColumnName("full_name");
        builder.Property(e => e.Phone)
            .HasMaxLength(15)
            .HasColumnName("phone");
        builder.Property(e => e.SupervisorNo)
            .HasMaxLength(25)
            .HasColumnName("supervisor_no");
        builder.Property(e => e.TitlePrefix)
            .HasMaxLength(50)
            .HasColumnName("title_prefix");

        builder.HasOne(d => d.ProjectProposal)
            .WithMany(p => p.ProposalSupervisors)
            .HasForeignKey(d => d.ProjectProposalId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_ProposalSupervisor_ProposalId");
    }
}