using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace S379658_HIT339_Assignment2_Library.Models.Configuration;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.Property(b => b.Author).HasColumnName("BookAuthor").HasMaxLength(150).IsRequired();
        builder.Property(b => b.Genre).HasColumnName("BookGenre").HasMaxLength(100).IsRequired();
    }
}
