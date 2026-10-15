using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace S379658_HIT339_Assignment2_Library.Models.Configuration;

internal sealed class ToyConfiguration : IEntityTypeConfiguration<Toy>
{
    public void Configure(EntityTypeBuilder<Toy> builder)
    {
        builder.Property(t => t.Type).HasColumnName("ToyType").HasMaxLength(100).IsRequired();
        builder.Property(t => t.MinimumAge).HasColumnName("ToyMinimumAge");
    }
}
