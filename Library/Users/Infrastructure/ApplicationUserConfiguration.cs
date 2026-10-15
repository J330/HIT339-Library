using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using S379658_HIT339_Assignment2_Library.Users.Domain;

namespace S379658_HIT339_Assignment2_Library.Users.Infrastructure;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {

        builder.Property(u => u.FullName).HasMaxLength(100);
    }
}
