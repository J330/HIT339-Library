using System.ComponentModel.DataAnnotations;
namespace S379658_HIT339_Assignment2_Library.ViewModels;
public sealed class TransferRequestViewModel
{
    [Range(1, int.MaxValue)] public int ItemId { get; set; }
    [Range(1, int.MaxValue)] public int DestinationBranchId { get; set; }
    [StringLength(500)] public string Notes { get; set; } = "";
}
