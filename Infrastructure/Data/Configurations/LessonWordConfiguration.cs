using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class LessonWordConfiguration : IEntityTypeConfiguration<LessonWord>
    {
        public void Configure(EntityTypeBuilder<LessonWord> builder)
        {
            builder.HasKey(x => new { x.LessonId, x.WordId });

            builder.HasOne(x => x.Lesson)
                .WithMany(l => l.LessonWords)
                .HasForeignKey(x => x.LessonId);

            builder.HasOne(x => x.Word)
                .WithMany(w => w.LessonWords)
                .HasForeignKey(x=>x.WordId);
        }
    }
}
