using S379658_HIT339_Assignment2_Library.Models;
namespace S379658_HIT339_Assignment2_Library.ViewModels;
public sealed class NotificationCalendarEventViewModel
{
    public DateTime Date { get; set; }
    public string Kind { get; set; } = "";
    public string Title { get; set; } = "";
    public string Details { get; set; } = "";
    public string? Borrower { get; set; }
}
public sealed class NotificationCalendarDayViewModel
{
    public DateTime Date { get; set; }
    public bool InMonth { get; set; }
    public List<NotificationCalendarEventViewModel> Events { get; set; } = new();
}
public sealed class CalendarPickerOption
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
}
public sealed class NotificationCalendarViewModel
{
    public bool StaffView { get; set; }
    public int? MemberId { get; set; }
    public List<CalendarPickerOption> Borrowers { get; set; } = new();
    public List<CalendarPickerOption> ReservableItems { get; set; } = new();
    public DateTime Month { get; set; }
    public DateTime SelectedDate { get; set; }
    public List<NotificationCalendarDayViewModel> Days { get; set; } = new();
    public List<NotificationCalendarEventViewModel> SelectedEvents { get; set; } = new();
    public List<LibraryNotification> History { get; set; } = new();
}
