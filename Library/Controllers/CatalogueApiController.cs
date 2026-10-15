using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;

namespace S379658_HIT339_Assignment2_Library.Controllers;

// Read-only public API. These response DTOs deliberately exclude borrower information,
// Identity records, internal database navigation objects and physical barcode codes.
[ApiController]
[Route("api/catalogue")]
[AllowAnonymous]
[EnableRateLimiting("public-api")]
public sealed class CatalogueApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public CatalogueApiController(AppDbContext db) => _db = db;

    [HttpGet("items")]
    public async Task<IActionResult> Items(
        [FromQuery] string? search = null, [FromQuery] string? type = null,
        [FromQuery] string? category = null, [FromQuery] string? availability = null,
        [FromQuery] int? branchId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 50)
            return BadRequest(new { error = "page must be >= 1 and pageSize must be from 1 to 50." });

        IQueryable<Item> query = _db.Items.AsNoTracking().Include(i => i.Branch);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i => i.Name.Contains(term) || i.Description.Contains(term) ||
                (i is Book && (((Book)i).Author.Contains(term) || ((Book)i).Genre.Contains(term))) ||
                (i is Music && (((Music)i).Artist.Contains(term) || ((Music)i).Genre.Contains(term))) ||
                (i is Toy && ((Toy)i).Type.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = type.Trim().ToLowerInvariant() switch
            {
                "book" => query.Where(i => i is Book),
                "music" => query.Where(i => i is Music),
                "toy" => query.Where(i => i is Toy),
                _ => query.Where(i => false)
            };
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var c = category.Trim();
            query = query.Where(i =>
                (i is Book && ((Book)i).Genre.Contains(c)) ||
                (i is Music && ((Music)i).Genre.Contains(c)) ||
                (i is Toy && ((Toy)i).Type.Contains(c)));
        }

        if (!string.IsNullOrWhiteSpace(availability))
        {
            if (!Enum.TryParse<ItemStatus>(availability, true, out var status) ||
                !Enum.IsDefined(status))
                return BadRequest(new { error = "Unknown availability. Use Available, Borrowed, Damaged, or Destroyed." });
            query = query.Where(i => i.Status == status);
        }

        if (branchId.HasValue)
        {
            if (branchId.Value < 1) return BadRequest(new { error = "branchId must be positive." });
            query = query.Where(i => i.BranchId == branchId.Value);
        }

        var total = await query.CountAsync();
        var offset = ((long)page - 1) * pageSize;
        var items = offset >= total ? new List<Item>() : await query
            .OrderBy(i => i.Name).ThenBy(i => i.Id)
            .Skip((int)offset).Take(pageSize).ToListAsync();

        var pageIds = items.Select(i => i.Id).ToList();
        var heldIds = (await _db.Reservations.AsNoTracking()
            .Where(r => pageIds.Contains(r.ItemId) && r.Status == ReservationStatus.Ready)
            .Select(r => r.ItemId).ToListAsync()).ToHashSet();
        return Ok(new { page, pageSize, total, items = items.Select(i => MapItem(i, heldIds.Contains(i.Id))) });
    }

    [HttpGet("items/{id:int}")]
    public async Task<IActionResult> ItemById(int id)
    {
        var item = await _db.Items.AsNoTracking().Include(i => i.Branch).FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return NotFound(new { error = "Item not found." });
        var held = await _db.Reservations.AnyAsync(r => r.ItemId == id && r.Status == ReservationStatus.Ready);
        return Ok(MapItem(item, held));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories()
    {
        var books = await _db.Books.AsNoTracking().Select(b => b.Genre).Distinct().ToListAsync();
        var music = await _db.Music.AsNoTracking().Select(m => m.Genre).Distinct().ToListAsync();
        var toys = await _db.Toys.AsNoTracking().Select(t => t.Type).Distinct().ToListAsync();
        return Ok(new
        {
            books = books.Where(s => !string.IsNullOrWhiteSpace(s)).OrderBy(s => s),
            music = music.Where(s => !string.IsNullOrWhiteSpace(s)).OrderBy(s => s),
            toys = toys.Where(s => !string.IsNullOrWhiteSpace(s)).OrderBy(s => s)
        });
    }

    [HttpGet("branches")]
    public async Task<IActionResult> Branches() => Ok(await _db.LibraryBranches.AsNoTracking()
        .Where(b => b.IsActive).OrderBy(b => b.Name)
        .Select(b => new { b.Id, b.Name, b.Code, b.Address, b.OpeningHours }).ToListAsync());

    [HttpGet("status")]
    public async Task<IActionResult> Status() => Ok(new
    {
        apiStatus = "online",
        catalogueSearch = "available",
        serviceTimeUtc = DateTimeOffset.UtcNow,
        branches = await _db.LibraryBranches.AsNoTracking().Where(b => b.IsActive)
            .Select(b => new { b.Name, b.OpeningHours }).ToListAsync()
    });

    private static object MapItem(Item item, bool held) => new
    {
        id = item.Id,
        title = item.Name,
        description = item.Description,
        type = item switch { Book => "Book", Music => "Music", Toy => "Toy", _ => "Item" },
        category = item switch { Book b => b.Genre, Music m => m.Genre, Toy t => t.Type, _ => "" },
        creator = item switch { Book b => b.Author, Music m => m.Artist, _ => "" },
        availability = item.Status.ToString(),
        branch = item.Branch?.Name,
        heldForCollection = held,
        canCheckout = item.Status == ItemStatus.Available && !held
    };
}
