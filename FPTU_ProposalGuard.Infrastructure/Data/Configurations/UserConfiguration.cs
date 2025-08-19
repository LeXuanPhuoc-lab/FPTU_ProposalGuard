using FPTU_ProposalGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ProposalGuard.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.UserId).HasName("PK_User_UserId");

        builder.ToTable("User");

        builder.Property(e => e.UserId)
            .HasDefaultValueSql("(newsequentialid())")
            .HasColumnName("user_id");
        builder.Property(e => e.Avatar)
            .HasMaxLength(2048)
            .IsUnicode(false)
            .HasColumnName("avatar");
        builder.Property(e => e.CreateDate)
            .HasColumnType("datetime")
            .HasColumnName("create_date");
        builder.Property(e => e.Dob)
            .HasColumnType("datetime")
            .HasColumnName("dob");
        builder.Property(e => e.Email)
            .HasMaxLength(255)
            .HasColumnName("email");
        builder.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
        builder.Property(e => e.FirstName)
            .HasMaxLength(100)
            .HasColumnName("first_name");
        builder.Property(e => e.Gender)
            .HasMaxLength(50)
            .HasColumnName("gender");
        builder.Property(e => e.Address)
            .HasMaxLength(255)
            .HasColumnName("address");
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasColumnName("is_active");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
        builder.Property(e => e.LastName)
            .HasMaxLength(100)
            .HasColumnName("last_name");
        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(100)
            .HasColumnName("modified_by");
        builder.Property(e => e.ModifiedDate)
            .HasColumnType("datetime")
            .HasColumnName("modified_date");
        builder.Property(e => e.PasswordHash)
            .HasMaxLength(255)
            .HasColumnName("password_hash");
        builder.Property(e => e.Phone)
            .HasMaxLength(15)
            .HasColumnName("phone");
        builder.Property(e => e.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
        builder.Property(e => e.PhoneVerificationCode)
            .HasMaxLength(20)
            .HasColumnName("phone_verification_code");
        builder.Property(e => e.EmailVerificationCode)
            .HasMaxLength(100)
            .HasColumnName("email_verification_code");
        builder.Property(e => e.TwoFactorBackupCodes)
            .HasMaxLength(255)
            .HasColumnName("two_factory_backup_codes");
        builder.Property(e => e.PhoneVerificationExpiry)
            .HasColumnType("datetime")
            .HasColumnName("phone_verification_expiry");
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.TwoFactorBackupCodes).HasColumnName("two_factor_backup_codes");
        builder.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
        builder.Property(e => e.TwoFactorSecretKey)
            .HasMaxLength(255)
            .HasColumnName("two_factor_secret_key");

        builder.HasOne(d => d.Role).WithMany(p => p.Users)
            .HasForeignKey(d => d.RoleId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_User_RoleId");
        builder.HasMany(u => u.ReviewSessions)
            .WithOne(s => s.Reviewer)
            .HasForeignKey(s => s.ReviewerId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_ReviewSession_ReviewerId");
    }
}