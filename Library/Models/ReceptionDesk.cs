using System.ComponentModel.DataAnnotations;
namespace S379658_HIT339_Assignment2_Library.Models;
public class ReceptionDesk
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Name { get; set; } = "";
    public int BranchId { get; set; }
    public LibraryBranch Branch { get; set; } = null!;
}
