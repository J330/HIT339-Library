using System.ComponentModel.DataAnnotations;
namespace S379658_HIT339_Assignment2_Library.Models;
public class LibraryBranch
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(16)] public string Code { get; set; } = "";
    [Required, StringLength(200)] public string Address { get; set; } = "";
    [StringLength(40)] public string Phone { get; set; } = "";
    [StringLength(150)] public string OpeningHours { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<ReceptionDesk> ReceptionDesks { get; set; } = new List<ReceptionDesk>();
}
