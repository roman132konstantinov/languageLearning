using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data.Configurations
{
    public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
    {
        public void Configure(EntityTypeBuilder<Exercise> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Question)
                .IsRequired();

            builder.HasOne(x => x.Lesson)
                .WithMany(l => l.Exercises)
                .HasForeignKey(x => x.LessonId);
            builder.HasOne(x => x.Word)
               .WithMany(x => x.Exercises)
              .HasForeignKey(x => x.WordId)
              .OnDelete(DeleteBehavior.SetNull);

        }
    }
}
