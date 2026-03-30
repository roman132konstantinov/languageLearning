using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data.Configurations
{
    public class ExerciseOptionConfiguration : IEntityTypeConfiguration<ExerciseOption>
    {
        public void Configure(EntityTypeBuilder<ExerciseOption> builder)
        {
            builder.HasKey(x => x.Id); ;

            builder.Property(x => x.Text)
                .IsRequired();

            builder.HasOne(x => x.Exercise)
                .WithMany(e => e.Options)
                .HasForeignKey(x => x.ExerciseId);

        }
    }
}
