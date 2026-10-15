using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Services;

// Import is intentionally all-or-nothing: invalid rows do not create partial inventory.
public sealed class MediaImportService
{
    private const int MaxRows = 500;
    private const long MaxBytes = 2 * 1024 * 1024;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private static readonly string[] Headers =
    ["Type", "LibraryCode", "Name", "Description", "BranchCode", "Author", "Genre", "Artist", "Year", "ToyType", "MinimumAge"];

    public MediaImportService(AppDbContext db, IWebHostEnvironment environment)
    { _db = db; _environment = environment; }

    public async Task<MediaImportResult> ImportCsvAsync(IFormFile? file)
    {
        var result = new MediaImportResult();
        if (file is null || file.Length == 0) { result.Errors.Add("Select a non-empty CSV file."); return result; }
        if (file.Length > MaxBytes) { result.Errors.Add("CSV must be 2 MB or smaller."); return result; }
        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        { result.Errors.Add("Only .csv files are accepted."); return result; }

        var rows = new List<MediaImportRow>();
        try
        {
            await using var upload = file.OpenReadStream();
            using var reader = new StreamReader(upload);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            if (!await csv.ReadAsync()) { result.Errors.Add("CSV is empty."); return result; }
            csv.ReadHeader();
            var actualHeaders = csv.HeaderRecord ?? [];
            var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < actualHeaders.Length; i++)
                if (!indices.TryAdd(actualHeaders[i].Trim(), i)) result.Errors.Add("Duplicate CSV column: " + actualHeaders[i]);
            foreach (var header in Headers)
                if (!indices.ContainsKey(header)) result.Errors.Add("Missing column: " + header);
            if (result.Errors.Count > 0) return result;
            while (await csv.ReadAsync())
            {
                result.RowsRead++;
                if (result.RowsRead > MaxRows)
                { result.Errors.Add("A file may contain at most 500 data rows."); return result; }
                string Field(string key) => (csv.GetField(indices[key]) ?? "").Trim();
                rows.Add(new MediaImportRow
                {
                    Type = Field("Type"), LibraryCode = Field("LibraryCode"), Name = Field("Name"),
                    Description = Field("Description"), BranchCode = Field("BranchCode"),
                    Author = Field("Author"), Genre = Field("Genre"), Artist = Field("Artist"),
                    Year = Field("Year"), ToyType = Field("ToyType"), MinimumAge = Field("MinimumAge")
                });
            }
        }
        catch (Exception ex) when (ex is CsvHelperException or IOException or DecoderFallbackException or ArgumentException)
        { result.Errors.Add("Unable to read CSV: " + ex.Message); return result; }
        return await ValidateAndSaveAsync(rows, result);
    }

    public async Task<MediaImportResult> ImportMockAsync(int branchId)
    {
        var result = new MediaImportResult();
        var branch = await _db.LibraryBranches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId && b.IsActive);
        if (branch is null) { result.Errors.Add("Select an active branch."); return result; }
        var path = Path.Combine(_environment.ContentRootPath, "Resources", "mock-media.json");
        var rows = JsonSerializer.Deserialize<List<MediaImportRow>>(await File.ReadAllTextAsync(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        foreach (var row in rows) row.BranchCode = branch.Code;
        result.RowsRead = rows.Count;
        return await ValidateAndSaveAsync(rows, result);
    }

    // A selected external work/release is metadata, not an inventory copy. Staff choose a
    // unique barcode/code and holding branch. Multiple legitimate copies may share a title.
    public async Task<MediaImportResult> ImportExternalAsync(
        ExternalMediaRecord record, ExternalImportForm form)
    {
        var result = new MediaImportResult { RowsRead = 1 };
        var code = (form.LibraryCode ?? "").Trim();
        var creator = (form.Creator ?? "").Trim();
        var genre = (form.Genre ?? "").Trim();
        var description = (form.Description ?? "").Trim();

        if (code.Length is < 1 or > 50) result.Errors.Add("Library code must be 1–50 characters.");
        if (await _db.Items.AnyAsync(i => i.LibraryCode == code))
            result.Errors.Add("That physical copy code is already in use.");
        if (string.IsNullOrWhiteSpace(record.Title) || record.Title.Length > 150)
            result.Errors.Add("External item has an invalid title.");
        if (record.Type != "Toy" && (creator.Length < 1 || creator.Length > 150)) result.Errors.Add("Author/artist is required (max 150 characters).");
        if (genre.Length is < 1 or > 100) result.Errors.Add("Genre/category is required (max 100 characters).");
        if (description.Length > 1000) result.Errors.Add("Description is too long.");
        if (!await _db.LibraryBranches.AnyAsync(b => b.Id == form.BranchId && b.IsActive))
            result.Errors.Add("Select an active holding branch.");
        Item? item = record.Type switch
        {
            "Book" => new Book { Author = creator, Genre = genre },
            "Music" => new Music { Artist = creator, Genre = genre, Year = form.Year ?? 0 },
            "Toy" => new Toy { Type = genre, MinimumAge = form.MinimumAge ?? -1 },
            _ => null
        };
        if (item is null) result.Errors.Add("Unsupported media type.");
        if (record.Type == "Music" && (form.Year is null || form.Year < 1000 || form.Year > DateTime.UtcNow.Year + 1))
            result.Errors.Add("Music must have a valid release year.");
        if (record.Type == "Toy" && (form.MinimumAge is null || form.MinimumAge < 0 || form.MinimumAge > 18))
            result.Errors.Add("Toys require a minimum age from 0 to 18.");
        if (result.Errors.Count > 0 || item is null) return result;

        item.Name = record.Title;
        item.Description = description;
        item.LibraryCode = code;
        item.BranchId = form.BranchId;
        item.Status = ItemStatus.Available;
        _db.Items.Add(item);
        try
        {
            await _db.SaveChangesAsync();
            result.Imported = 1;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            result.Errors.Add("Could not save this item. Check that its code is unique and the branch still exists.");
        }
        return result;
    }

    private async Task<MediaImportResult> ValidateAndSaveAsync(List<MediaImportRow> rows, MediaImportResult result)
    {
        if (rows.Count == 0) { result.Errors.Add("There are no items to import."); return result; }
        if (rows.Count > MaxRows) { result.Errors.Add("Too many items to import."); return result; }
        var branches = await _db.LibraryBranches.AsNoTracking().Where(b => b.IsActive).ToListAsync();
        var branchMap = branches.ToDictionary(b => b.Code, StringComparer.OrdinalIgnoreCase);
        var existingCodes = (await _db.Items.AsNoTracking().Select(i => i.LibraryCode).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entities = new List<Item>();

        for (int n = 0; n < rows.Count; n++)
        {
            var row = rows[n];
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(row.LibraryCode) || row.LibraryCode.Length > 50) errors.Add("LibraryCode is required (max 50)");
            if (row.LibraryCode.Length > 0 && (!seen.Add(row.LibraryCode) || existingCodes.Contains(row.LibraryCode)))
                errors.Add("duplicate LibraryCode '" + row.LibraryCode + "'");
            if (string.IsNullOrWhiteSpace(row.Name) || row.Name.Length > 150) errors.Add("Name is required (max 150)");
            if (row.Description.Length > 1000) errors.Add("Description exceeds 1000 characters");
            if (!branchMap.TryGetValue(row.BranchCode, out var branch)) errors.Add("unknown or inactive BranchCode");
            Item? item = null;
            switch (row.Type.ToLowerInvariant())
            {
                case "book":
                    if (string.IsNullOrWhiteSpace(row.Author) || row.Author.Length > 150) errors.Add("Author is required (max 150)");
                    if (string.IsNullOrWhiteSpace(row.Genre) || row.Genre.Length > 100) errors.Add("Genre is required (max 100)");
                    item = new Book { Author = row.Author, Genre = row.Genre }; break;
                case "music":
                    if (string.IsNullOrWhiteSpace(row.Artist) || row.Artist.Length > 150) errors.Add("Artist is required (max 150)");
                    if (string.IsNullOrWhiteSpace(row.Genre) || row.Genre.Length > 100) errors.Add("Genre is required (max 100)");
                    if (!int.TryParse(row.Year, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year) ||
                        year < 1000 || year > DateTime.UtcNow.Year + 1) errors.Add("Year is invalid");
                    item = new Music { Artist = row.Artist, Genre = row.Genre, Year = year }; break;
                case "toy":
                    if (string.IsNullOrWhiteSpace(row.ToyType) || row.ToyType.Length > 100) errors.Add("ToyType is required (max 100)");
                    if (!int.TryParse(row.MinimumAge, NumberStyles.Integer, CultureInfo.InvariantCulture, out var age) ||
                        age < 0 || age > 18) errors.Add("MinimumAge must be 0–18");
                    item = new Toy { Type = row.ToyType, MinimumAge = age }; break;
                default: errors.Add("Type must be Book, Music or Toy"); break;
            }
            if (errors.Count > 0)
            {
                if (result.Errors.Count < 30) result.Errors.Add($"Row {n + 2}: {string.Join("; ", errors)}");
            }
            else if (item is not null && branch is not null)
            {
                item.Name = row.Name;
                item.Description = row.Description;
                item.LibraryCode = row.LibraryCode;
                item.BranchId = branch.Id;
                item.Status = ItemStatus.Available;
                entities.Add(item);
            }
        }
        if (result.Errors.Count > 0) return result;
        _db.Items.AddRange(entities);
        try
        {
            await _db.SaveChangesAsync();
            result.Imported = entities.Count;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            result.Errors.Add("Database rejected the import (possibly a duplicate code). No new items were saved.");
        }
        return result;
    }
}
