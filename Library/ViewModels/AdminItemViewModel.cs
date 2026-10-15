using System.ComponentModel.DataAnnotations;
using S379658_HIT339_Assignment2_Library.Models;

namespace S379658_HIT339_Assignment2_Library.ViewModels;

public class AdminItemViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Item Type")]
    public LibraryItemType ItemType { get; set; } = LibraryItemType.Book;

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Library Code")]
    public string LibraryCode { get; set; } = string.Empty;

    public ItemStatus Status { get; set; } = ItemStatus.Available;
    [Range(1, int.MaxValue, ErrorMessage = "Select a library branch.")]
    [Display(Name = "Holding Branch")]
    public int BranchId { get; set; }

    [StringLength(150)]
    public string? Author { get; set; }

    [StringLength(100)]
    public string? Genre { get; set; }

    [StringLength(150)]
    public string? Artist { get; set; }

    [Range(1000, 3000, ErrorMessage = "Enter a valid year.")]
    public int? Year { get; set; }

    [Display(Name = "Toy Type")]
    [StringLength(100)]
    public string? ToyType { get; set; }

    [Display(Name = "Minimum Age")]
    [Range(0, 100, ErrorMessage = "Minimum age must be between 0 and 100.")]
    public int? MinimumAge { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        switch (ItemType)
        {
            case LibraryItemType.Book:
                if (string.IsNullOrWhiteSpace(Author))
                    yield return new ValidationResult("Author is required for a book.", new[] { nameof(Author) });
                if (string.IsNullOrWhiteSpace(Genre))
                    yield return new ValidationResult("Genre is required for a book.", new[] { nameof(Genre) });
                break;

            case LibraryItemType.Music:
                if (string.IsNullOrWhiteSpace(Artist))
                    yield return new ValidationResult("Artist is required for music.", new[] { nameof(Artist) });
                if (Year == null)
                    yield return new ValidationResult("Year is required for music.", new[] { nameof(Year) });
                if (string.IsNullOrWhiteSpace(Genre))
                    yield return new ValidationResult("Genre is required for music.", new[] { nameof(Genre) });
                break;

            case LibraryItemType.Toy:
                if (string.IsNullOrWhiteSpace(ToyType))
                    yield return new ValidationResult("Toy type is required.", new[] { nameof(ToyType) });
                if (MinimumAge == null)
                    yield return new ValidationResult("Minimum age is required for a toy.", new[] { nameof(MinimumAge) });
                break;

            default:
                yield return new ValidationResult("Please select a valid item type.", new[] { nameof(ItemType) });
                break;
        }
    }
}
