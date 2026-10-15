using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace S379658_HIT339_Assignment2_Library.Models.Configuration;

internal sealed class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
    public void Configure(EntityTypeBuilder<Borrowing> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.FineAmount).HasPrecision(18, 2);

        builder.HasOne(b => b.Borrower)
            .WithMany(b => b.Borrowings)
            .HasForeignKey(b => b.BorrowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Item)
            .WithMany(i => i.Borrowings)
            .HasForeignKey(b => b.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // SQL Server enforces one active (not-returned) loan per physical item.
        builder.HasIndex(b => b.ItemId)
            .IsUnique()
            .HasFilter("[ReturnDate] IS NULL");

        builder.HasOne(b => b.CheckoutBranch).WithMany()
            .HasForeignKey(b => b.CheckoutBranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.ReturnBranch).WithMany()
            .HasForeignKey(b => b.ReturnBranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(b => b.BorrowerId);
        builder.HasIndex(b => b.DueDate);
    }
}
