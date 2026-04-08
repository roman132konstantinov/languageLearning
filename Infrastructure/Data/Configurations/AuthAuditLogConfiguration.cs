using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class AuthAuditLogConfiguration : IEntityTypeConfiguration<AuthAuditLog>
    {
        public void Configure(EntityTypeBuilder<AuthAuditLog> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Email)
                .HasMaxLength(150);

            builder.Property(x => x.FailureReason)
                .HasMaxLength(512);

            builder.Property(x => x.IpAddress)
                .HasMaxLength(128);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(512);

            builder.Property(x => x.SessionId)
                .HasMaxLength(64);

            builder.HasIndex(x => new { x.UserId, x.CreatedAt });
            builder.HasIndex(x => new { x.EventType, x.CreatedAt });
        }
    }
}
