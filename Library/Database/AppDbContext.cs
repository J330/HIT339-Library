using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Users.Domain;

namespace S379658_HIT339_Assignment2_Library.Database;

public sealed class AppDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Item> Items { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Music> Music { get; set; }
    public DbSet<Toy> Toys { get; set; }
    public DbSet<Borrower> Borrowers { get; set; }
    public DbSet<Borrowing> Borrowings { get; set; }
    public DbSet<LibraryBranch> LibraryBranches { get; set; }
    public DbSet<ReceptionDesk> ReceptionDesks { get; set; }
    public DbSet<ItemTransfer> ItemTransfers { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<LibraryNotification> LibraryNotifications { get; set; }
    public DbSet<FinePayment> FinePayments { get; set; }
    public DbSet<CalendarEntry> CalendarEntries { get; set; }
    public DbSet<CalendarEntryRecipient> CalendarEntryRecipients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity must configure its User/Role/Claim/Login/Token relationships first.
        base.OnModelCreating(modelBuilder);

        // Keeps application entity rules in small configuration classes.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
