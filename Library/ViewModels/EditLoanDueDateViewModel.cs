using System.ComponentModel.DataAnnotations;
namespace S379658_HIT339_Assignment2_Library.ViewModels;
public sealed class EditLoanDueDateViewModel
{
    public int Id { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "New due date")]
    public DateTime DueDate { get; set; }
    public DateTime BorrowDate { get; set; }
    public string ItemName { get; set; } = "";
    public string BorrowerName { get; set; } = "";
}
