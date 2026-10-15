using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace S379658_HIT339_Assignment2_Library.Models.Configuration;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(i => i.Description)
            .HasMaxLength(1000);

        builder.Property(i => i.LibraryCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property<string>("Discriminator")
            .HasMaxLength(13);

        builder.HasIndex(i => i.LibraryCode)
            .IsUnique();
        builder.HasIndex(i => i.BranchId);
        builder.HasOne(i => i.Branch).WithMany(b => b.Items)
            .HasForeignKey(i => i.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
