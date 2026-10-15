namespace S379658_HIT339_Assignment2_Library.Services;

public sealed class FineService
{
    public const decimal DailyFineRate = 1.00m;

    public decimal CalculateFine(DateTime dueDate, DateTime returnDate)
    {
        var daysLate = (returnDate.Date - dueDate.Date).Days;
        return daysLate > 0 ? daysLate * DailyFineRate : 0m;
    }
}
