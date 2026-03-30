using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data.Configurations
{
    public class UserLessonProgressConfiguration : IEntityTypeConfiguration<UserLessonProgress>
    {
        public void Configure(EntityTypeBuilder<UserLessonProgress> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.User)
                .WithMany(u => u.UserLessonProgresses)
                .HasForeignKey(x => x.UserId);

            builder.HasOne(x => x.Lesson)
                .WithMany(l => l.UserLessonProgresses)
                .HasForeignKey(x => x.LessonId);

            builder.HasIndex(x => new { x.UserId, x.LessonId })
                .IsUnique();
        }
    }
}
