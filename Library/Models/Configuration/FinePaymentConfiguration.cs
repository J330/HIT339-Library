using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace S379658_HIT339_Assignment2_Library.Models.Configuration;
internal sealed class FinePaymentConfiguration : IEntityTypeConfiguration<FinePayment>
{
    public void Configure(EntityTypeBuilder<FinePayment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Reference).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.BorrowingId);
        builder.HasIndex(p => p.PaidAtUtc);
        builder.HasOne(p => p.Borrowing).WithMany().HasForeignKey(p => p.BorrowingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
