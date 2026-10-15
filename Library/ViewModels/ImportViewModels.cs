using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace S379658_HIT339_Assignment2_Library.ViewModels;

public sealed class MediaImportForm
{
    [Display(Name = "CSV file (max 2 MB / 500 rows)")]
    public IFormFile? File { get; set; }
    [Display(Name = "Branch for mock catalogue")]
    public int BranchId { get; set; }
}

public sealed class MediaImportResult
{
    public int RowsRead { get; set; }
    public int Imported { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Success => Errors.Count == 0;
}

public sealed class MediaImportRow
{
    public string Type { get; set; } = "";
    public string LibraryCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string BranchCode { get; set; } = "";
    public string Author { get; set; } = "";
    public string Genre { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Year { get; set; } = "";
    public string ToyType { get; set; } = "";
    public string MinimumAge { get; set; } = "";
}

public sealed class ExternalSearchViewModel
{
    public string Provider { get; set; } = "OpenLibrary";
    public string SearchType { get; set; } = "keyword";
    public string Query { get; set; } = "";
    public List<S379658_HIT339_Assignment2_Library.Services.ExternalMediaRecord> Results { get; set; } = new();
    public string? Error { get; set; }
    public bool Searched { get; set; }
}

public sealed class ExternalImportForm
{
    [Required] public string Provider { get; set; } = "";
    [Required] public string ExternalId { get; set; } = "";
    [Required(ErrorMessage = "Select an active library branch.")]
    [Range(1, int.MaxValue, ErrorMessage = "Select an active library branch.")]
    [Display(Name = "Holding branch")]
    public int BranchId { get; set; }
    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "New physical copy code")]
    public string LibraryCode { get; set; } = "";
    [StringLength(150)]
    [Display(Name = "Author / artist")]
    public string Creator { get; set; } = "";
    [Required, StringLength(100)]
    [Display(Name = "Genre / toy category")]
    public string Genre { get; set; } = "";
    [StringLength(1000)]
    [Display(Name = "Catalogue description")]
    public string Description { get; set; } = "";
    [Range(1000, 3000)]
    [Display(Name = "Publication / release year (music)")]
    public int? Year { get; set; }
    [Range(0, 18)]
    [Display(Name = "Minimum age (toy)")]
    public int? MinimumAge { get; set; }
}
