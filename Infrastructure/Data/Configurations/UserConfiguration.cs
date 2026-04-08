using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.PasswordHash)
                .IsRequired();

            builder.Property(x => x.UserName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Role)
                .IsRequired();

            builder.Property(x => x.SecurityStamp)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.EmailVerificationTokenHash)
                .HasMaxLength(256);

            builder.Property(x => x.PasswordResetTokenHash)
                .HasMaxLength(256);

            builder.HasIndex(x => x.Email).IsUnique();

            builder.HasMany(x => x.RefreshTokens)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.AuthAuditLogs)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
