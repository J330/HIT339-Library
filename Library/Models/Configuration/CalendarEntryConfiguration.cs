using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace S379658_HIT339_Assignment2_Library.Models.Configuration;
internal sealed class CalendarEntryConfiguration : IEntityTypeConfiguration<CalendarEntry>
{
    public void Configure(EntityTypeBuilder<CalendarEntry> builder)
    {
        builder.Property(e => e.ScheduledDate).HasColumnType("date");
        builder.Property(e => e.Title).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(800).IsRequired();
        builder.Property(e => e.Audience).HasConversion<int>().HasDefaultValue(CalendarAudience.None);
        builder.HasOne(e => e.Borrower).WithMany().HasForeignKey(e => e.BorrowerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Reservation).WithMany().HasForeignKey(e => e.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.ScheduledDate, e.Kind });
        builder.HasIndex(e => new { e.BorrowerId, e.ScheduledDate });
    }
}
