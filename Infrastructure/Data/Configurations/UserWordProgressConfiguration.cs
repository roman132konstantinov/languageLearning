using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data.Configurations
{
    public class UserWordProgressConfiguration : IEntityTypeConfiguration<UserWordProgress>
    {
        public void Configure(EntityTypeBuilder<UserWordProgress> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.User)
                .WithMany(u => u.WordProgresses)
                .HasForeignKey(x => x.UserId);

            builder.HasOne(x => x.Word)
                .WithMany(w => w.UserWordProgresses)
                .HasForeignKey(x => x.WordId);

            builder.HasIndex(x => new { x.UserId, x.WordId })
                .IsUnique();
        }
    }
}
