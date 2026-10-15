using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace S379658_HIT339_Assignment2_Library.Models.Configuration;

internal sealed class MusicConfiguration : IEntityTypeConfiguration<Music>
{
    public void Configure(EntityTypeBuilder<Music> builder)
    {
        builder.Property(m => m.Artist).HasColumnName("MusicArtist").HasMaxLength(150).IsRequired();
        builder.Property(m => m.Genre).HasColumnName("MusicGenre").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Year).HasColumnName("MusicYear");
    }
}
