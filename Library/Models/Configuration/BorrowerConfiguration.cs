using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace S379658_HIT339_Assignment2_Library.Models.Configuration;

internal sealed class BorrowerConfiguration : IEntityTypeConfiguration<Borrower>
{
    public void Configure(EntityTypeBuilder<Borrower> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(b => b.LastName).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Email).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Phone).HasMaxLength(30).IsRequired();
        builder.Property(b => b.Address).HasMaxLength(200).IsRequired();
        builder.HasIndex(b => b.Email);
        builder.Property(b => b.MembershipCode).HasMaxLength(24).IsRequired();
        builder.HasIndex(b => b.MembershipCode).IsUnique();
        builder.HasIndex(b => b.IdentityUserId).IsUnique();
        builder.HasOne<S379658_HIT339_Assignment2_Library.Users.Domain.ApplicationUser>()
            .WithOne().HasForeignKey<Borrower>(b => b.IdentityUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
