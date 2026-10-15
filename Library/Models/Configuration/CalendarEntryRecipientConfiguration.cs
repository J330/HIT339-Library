using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace S379658_HIT339_Assignment2_Library.Models.Configuration;

internal sealed class CalendarEntryRecipientConfiguration : IEntityTypeConfiguration<CalendarEntryRecipient>
{
    public void Configure(EntityTypeBuilder<CalendarEntryRecipient> builder)
    {
        builder.HasKey(r => new { r.CalendarEntryId, r.BorrowerId });
        builder.HasOne(r => r.CalendarEntry).WithMany(e => e.Recipients)
            .HasForeignKey(r => r.CalendarEntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Borrower).WithMany()
            .HasForeignKey(r => r.BorrowerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.BorrowerId);
    }
}
