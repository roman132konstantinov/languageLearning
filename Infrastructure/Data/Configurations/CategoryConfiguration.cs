using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> Builder)
        {
            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

        }
    }
}
