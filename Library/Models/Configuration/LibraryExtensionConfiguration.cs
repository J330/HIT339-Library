using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace S379658_HIT339_Assignment2_Library.Models.Configuration;
internal sealed class BranchConfiguration : IEntityTypeConfiguration<LibraryBranch>
{
    public void Configure(EntityTypeBuilder<LibraryBranch> builder)
    {
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Code).HasMaxLength(16).IsRequired();
        builder.Property(b => b.Address).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Phone).HasMaxLength(40);
        builder.Property(b => b.OpeningHours).HasMaxLength(150);
        builder.HasIndex(b => b.Code).IsUnique();
    }
}
internal sealed class ReceptionDeskConfiguration : IEntityTypeConfiguration<ReceptionDesk>
{
    public void Configure(EntityTypeBuilder<ReceptionDesk> builder)
    {
        builder.Property(d => d.Name).HasMaxLength(80).IsRequired();
        builder.HasOne(d => d.Branch).WithMany(b => b.ReceptionDesks)
            .HasForeignKey(d => d.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
internal sealed class ItemTransferConfiguration : IEntityTypeConfiguration<ItemTransfer>
{
    public void Configure(EntityTypeBuilder<ItemTransfer> builder)
    {
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.HasOne(t => t.Item).WithMany(i => i.Transfers)
            .HasForeignKey(t => t.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.SourceBranch).WithMany()
            .HasForeignKey(t => t.SourceBranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.DestinationBranch).WithMany()
            .HasForeignKey(t => t.DestinationBranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.ItemId).IsUnique().HasFilter("[Status] = 0");
    }
}
internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasOne(r => r.Item).WithMany(i => i.Reservations)
            .HasForeignKey(r => r.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Borrower).WithMany(b => b.Reservations)
            .HasForeignKey(r => r.BorrowerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.ItemId, r.BorrowerId })
            .IsUnique().HasFilter("[Status] IN (0, 1)");
        builder.HasIndex(r => new { r.ItemId, r.Status, r.PlacedAtUtc, r.Id });
    }
}
internal sealed class LibraryNotificationConfiguration : IEntityTypeConfiguration<LibraryNotification>
{
    public void Configure(EntityTypeBuilder<LibraryNotification> builder)
    {
        builder.Property(n => n.Recipient).HasMaxLength(120).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(160).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.ReferenceKey).HasMaxLength(140).IsRequired();
        builder.Property(n => n.DeliveryStatus).HasMaxLength(24).IsRequired();
        builder.HasIndex(n => n.ReferenceKey).IsUnique();
        builder.HasOne(n => n.Borrower).WithMany(b => b.Notifications)
            .HasForeignKey(n => n.BorrowerId).OnDelete(DeleteBehavior.Restrict);
    }
}
