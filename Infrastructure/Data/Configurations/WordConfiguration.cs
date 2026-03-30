using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class WordConfiguration : IEntityTypeConfiguration<Word>
    {
        public void Configure(EntityTypeBuilder<Word> builder)

        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.KazakhText)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.RussianTranslation)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne(c => c.Category)
                .WithMany(w => w.Words)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
