namespace S379658_HIT339_Assignment2_Library.Models;

public class Music : Item
{
    public string Artist { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Genre { get; set; } = string.Empty;
}
