using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.Users.Domain;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Reception + "," + AppRoles.Manager + "," + AppRoles.Borrower)]
public sealed class NotificationsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly NotificationSweepService _sweep;
    private readonly ReservationService _reservations;
    public NotificationsController(AppDbContext db, UserManager<ApplicationUser> users, NotificationSweepService sweep, ReservationService reservations)
    { _db = db; _users = users; _sweep = sweep; _reservations = reservations; }

    public async Task<IActionResult> Index(string? month = null, string? day = null)
    {
        var staff = IsStaff();
        int? memberId = null;
        if (!staff)
        {
            var user = await _users.GetUserAsync(User);
            if (user is null || user.MustChangePassword) return Forbid();
            memberId = await _db.Borrowers.AsNoTracking().Where(b => b.IdentityUserId == user.Id)
                .Select(b => (int?)b.Id).SingleOrDefaultAsync();
            if (!memberId.HasValue) return Forbid();
        }

        var now = DateTime.Today;
        DateTime first;
        if (string.IsNullOrWhiteSpace(month)) first = new DateTime(now.Year, now.Month, 1);
        else if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out first)
                 || first.Year < 1900 || first.Year > 2099)
            return BadRequest("Invalid calendar month.");
        else first = new DateTime(first.Year, first.Month, 1);

        // Always display 6 complete Monday-to-Sunday weeks for consistent layout.
        var gridStart = first.AddDays(-(((int)first.DayOfWeek + 6) % 7));
        var gridEnd = gridStart.AddDays(42);
        var startUtc = gridStart.ToUniversalTime();
        var endUtc = gridEnd.ToUniversalTime();

        var loansQuery = _db.Borrowings.AsNoTracking().Include(b => b.Item).Include(b => b.Borrower)
            .Where(b => b.ReturnDate == null && b.DueDate >= gridStart && b.DueDate < gridEnd.AddDays(2));
        var holdsQuery = _db.Reservations.AsNoTracking().Include(r => r.Item).Include(r => r.Borrower)
            .Where(r => r.Status == ReservationStatus.Ready && r.ExpiresAtUtc >= startUtc && r.ExpiresAtUtc < endUtc);
        var notificationsQuery = _db.LibraryNotifications.AsNoTracking().Include(n => n.Borrower).AsQueryable();
        var paymentsQuery = _db.FinePayments.AsNoTracking()
            .Include(p => p.Borrowing).ThenInclude(b => b.Item)
            .Include(p => p.Borrowing).ThenInclude(b => b.Borrower)
            .AsQueryable();
        if (memberId.HasValue)
        {
            loansQuery = loansQuery.Where(b => b.BorrowerId == memberId.Value);
            holdsQuery = holdsQuery.Where(r => r.BorrowerId == memberId.Value);
            notificationsQuery = notificationsQuery.Where(n => n.BorrowerId == memberId.Value);
            paymentsQuery = paymentsQuery.Where(p => p.Borrowing.BorrowerId == memberId.Value);
        }

        var loans = await loansQuery.ToListAsync();
        var holds = await holdsQuery.ToListAsync();
        var logged = await notificationsQuery.Where(n => n.CreatedAtUtc >= startUtc && n.CreatedAtUtc < endUtc)
            .ToListAsync();
        var payments = await paymentsQuery.Where(p => p.PaidAtUtc >= startUtc && p.PaidAtUtc < endUtc)
            .ToListAsync();
        var calendarQuery = _db.CalendarEntries.AsNoTracking()
            .Include(e => e.Borrower)
            .Include(e => e.Recipients).ThenInclude(r => r.Borrower)
            .AsQueryable();
        if (staff)
        {
            // Managers, reception, and administrators only see events that include their role.
            // Other library records retain the staff-wide access they already had.
            var permittedRoles = CurrentAudience();
            calendarQuery = calendarQuery.Where(e => e.Kind != CalendarEntryKind.Event ||
                (e.Audience & permittedRoles) != CalendarAudience.None);
        }
        else
        {
            var ownBorrowerId = memberId!.Value;
            calendarQuery = calendarQuery.Where(e =>
                (e.Kind == CalendarEntryKind.Event &&
                    (e.Audience & CalendarAudience.Borrower) != CalendarAudience.None &&
                    (!e.Recipients.Any() || e.Recipients.Any(r => r.BorrowerId == ownBorrowerId))) ||
                (e.Kind != CalendarEntryKind.Event && e.BorrowerId == ownBorrowerId));
        }
        var calendarEntries = await calendarQuery.Where(e => e.ScheduledDate >= gridStart && e.ScheduledDate < gridEnd)
            .ToListAsync();
        var events = new List<NotificationCalendarEventViewModel>();
        void Add(DateTime date, string kind, string title, string details, string? borrower)
        {
            if (date.Date < gridStart || date.Date >= gridEnd) return;
            events.Add(new NotificationCalendarEventViewModel
            { Date = date.Date, Kind = kind, Title = title, Details = details, Borrower = staff ? borrower : null });
        }

        foreach (var loan in loans)
        {
            var who = loan.Borrower.FirstName + " " + loan.Borrower.LastName;
            Add(loan.DueDate.Date, "due", "Due: " + loan.Item.Name,
                "Loan return deadline. " + (loan.DueDate.Date < now ? "Overdue." : "Return by this date."), who);
            Add(loan.DueDate.Date.AddDays(-2), "reminder", "Reminder: " + loan.Item.Name,
                "Scheduled due-soon check two days before this loan is due (simulated email/SMS).", who);
        }
        foreach (var hold in holds)
        {
            // Hold deadlines are stored in UTC; display them in the server's local timezone.
            var expires = DateTime.SpecifyKind(hold.ExpiresAtUtc!.Value, DateTimeKind.Utc).ToLocalTime();
            Add(expires.Date, "hold", "Collect by: " + hold.Item.Name,
                "Reservation collection deadline: " + expires.ToString("dd MMM yyyy HH:mm") + " (server local time).",
                hold.Borrower.FirstName + " " + hold.Borrower.LastName);
        }
        // Existing simulated email + SMS entries are actual log entries, not future delivery promises.
        foreach (var entry in logged)
        {
            var created = DateTime.SpecifyKind(entry.CreatedAtUtc, DateTimeKind.Utc).ToLocalTime();
            Add(created.Date, "notification", entry.Subject + " (" + entry.Channel + ")",
                entry.Message + " — logged " + created.ToString("HH:mm") + " (server local time)",
                entry.Borrower.FirstName + " " + entry.Borrower.LastName);
        }

        // Fine payments appear on the date the payment was actually recorded, not the fine due date.
        foreach (var payment in payments)
        {
            var paid = DateTime.SpecifyKind(payment.PaidAtUtc, DateTimeKind.Utc).ToLocalTime();
            var who = payment.Borrowing.Borrower.FirstName + " " + payment.Borrowing.Borrower.LastName;
            var receipt = staff ? " Receipt: " + payment.Reference + "." : "";
            Add(paid.Date, "payment", "Fine payment: " + payment.Amount.ToString("C"),
                "Paid for " + payment.Borrowing.Item.Name + " at " + paid.ToString("HH:mm") +
                " (server local time)." + receipt, who);
        }

        foreach (var entry in calendarEntries)
        {
            string kind = entry.Kind switch
            {
                CalendarEntryKind.Event => "event",
                CalendarEntryKind.Notification => "scheduled",
                _ => "followup"
            };
            var status = entry.Kind == CalendarEntryKind.Notification
                ? (entry.DispatchedAtUtc.HasValue ? "Simulated notification logged." : "Scheduled for simulated delivery on or after this date.")
                : entry.Kind == CalendarEntryKind.ReservationReminder ? "Reservation follow-up (not a guaranteed collection date)." : "Calendar event.";
            Add(entry.ScheduledDate, kind, entry.Title, entry.Description + " " + status,
                entry.Kind == CalendarEntryKind.Event
                    ? DescribeAudience(entry)
                    : entry.Borrower == null ? null : entry.Borrower.FirstName + " " + entry.Borrower.LastName);
        }

        var selected = now;
        if (!string.IsNullOrWhiteSpace(day))
        {
            if (!DateTime.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out selected)
                || selected.Date < gridStart || selected.Date >= gridEnd)
                return BadRequest("Invalid calendar day.");
        }
        else if (now < gridStart || now >= gridEnd) selected = first;

        var vm = new NotificationCalendarViewModel
        {
            StaffView = staff,
            MemberId = memberId,
            Borrowers = staff ? await _db.Borrowers.AsNoTracking().OrderBy(b => b.LastName)
                .ThenBy(b => b.FirstName).Select(b => new CalendarPickerOption
                { Id = b.Id, Label = b.FirstName + " " + b.LastName + " (" + b.MembershipCode + ")" +
                    (b.IdentityUserId.HasValue ? " — " + (_db.Users.Where(u => u.Id == b.IdentityUserId.Value)
                        .Select(u => u.UserName).FirstOrDefault() ?? "") : "") }).ToListAsync()
                : new List<CalendarPickerOption>(),
            ReservableItems = await _db.Items.AsNoTracking()
                .Where(i => i.Status != ItemStatus.Destroyed)
                .OrderBy(i => i.Name)
                .Select(i => new CalendarPickerOption { Id = i.Id, Label = i.Name + " (" + i.LibraryCode + ") — " + i.Status })
                .ToListAsync(),
            Month = first,
            SelectedDate = selected.Date,
            History = await notificationsQuery.OrderByDescending(n => n.CreatedAtUtc).Take(100).ToListAsync(),
            SelectedEvents = events.Where(e => e.Date == selected.Date)
                .OrderBy(e => e.Kind).ThenBy(e => e.Title).ToList()
        };
        for (var date = gridStart; date < gridEnd; date = date.AddDays(1))
            vm.Days.Add(new NotificationCalendarDayViewModel
            {
                Date = date, InMonth = date.Month == first.Month,
                Events = events.Where(e => e.Date == date).OrderBy(e => e.Kind).ThenBy(e => e.Title).ToList()
            });
        return View(vm);
    }

    private bool IsStaff() => User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager) ||
        User.IsInRole(AppRoles.Reception);

    private CalendarAudience CurrentAudience()
    {
        var audience = CalendarAudience.None;
        if (User.IsInRole(AppRoles.Admin)) audience |= CalendarAudience.Admin;
        if (User.IsInRole(AppRoles.Manager)) audience |= CalendarAudience.Manager;
        if (User.IsInRole(AppRoles.Reception)) audience |= CalendarAudience.Reception;
        if (User.IsInRole(AppRoles.Borrower)) audience |= CalendarAudience.Borrower;
        return audience;
    }

    private static string DescribeAudience(CalendarEntry entry)
    {
        var recipients = new List<string>();
        if (entry.Audience.HasFlag(CalendarAudience.Admin)) recipients.Add("Administrators");
        if (entry.Audience.HasFlag(CalendarAudience.Manager)) recipients.Add("Managers");
        if (entry.Audience.HasFlag(CalendarAudience.Reception)) recipients.Add("Reception");
        if (entry.Audience.HasFlag(CalendarAudience.Borrower))
        {
            var named = entry.Recipients.OrderBy(r => r.Borrower.LastName)
                .ThenBy(r => r.Borrower.FirstName)
                .Select(r => r.Borrower.FirstName + " " + r.Borrower.LastName).ToList();
            recipients.Add(named.Count == 0 ? "All borrowers" : "Selected borrowers: " + string.Join(", ", named));
        }
        return string.Join(", ", recipients);
    }

    private static bool ParseDate(string date, out DateTime result) =>
        DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out result) && result.Year >= 1900 && result.Year <= 2099;

    private IActionResult Back(DateTime? date)
    {
        if (!date.HasValue) return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Index), new { month = date.Value.ToString("yyyy-MM"), day = date.Value.ToString("yyyy-MM-dd") });
    }

    private async Task<int?> CurrentMemberIdAsync()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null || user.MustChangePassword) return null;
        return await _db.Borrowers.AsNoTracking().Where(b => b.IdentityUserId == user.Id)
            .Select(b => (int?)b.Id).SingleOrDefaultAsync();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(string date, string title, string? description,
        int[]? borrowerIds, string[]? visibleToRoles)
    {
        if (!ParseDate(date, out var selected)) return BadRequest("Invalid calendar date.");
        var staff = IsStaff();
        var audience = CalendarAudience.None;
        int? creatorBorrowerId = null;
        int[] selectedBorrowers;
        if (!staff)
        {
            creatorBorrowerId = await CurrentMemberIdAsync();
            if (!creatorBorrowerId.HasValue) return Forbid();
            audience = CalendarAudience.Borrower;
            // Never trust a borrower-supplied audience or list of other borrowers.
            selectedBorrowers = new[] { creatorBorrowerId.Value };
        }
        else
        {
            foreach (var role in visibleToRoles ?? Array.Empty<string>())
            {
                audience |= role switch
                {
                    AppRoles.Admin => CalendarAudience.Admin,
                    AppRoles.Manager => CalendarAudience.Manager,
                    AppRoles.Reception => CalendarAudience.Reception,
                    AppRoles.Borrower => CalendarAudience.Borrower,
                    _ => CalendarAudience.None
                };
                if (role is not (AppRoles.Admin or AppRoles.Manager or AppRoles.Reception or AppRoles.Borrower))
                { TempData["ErrorMessage"] = "Unknown audience role."; return Back(selected); }
            }
            if (audience == CalendarAudience.None)
            { TempData["ErrorMessage"] = "Select at least one role that can see this event."; return Back(selected); }
            selectedBorrowers = (borrowerIds ?? Array.Empty<int>()).Distinct().ToArray();
            if (selectedBorrowers.Length > 200 || selectedBorrowers.Any(id => id <= 0))
            { TempData["ErrorMessage"] = "Choose up to 200 valid borrowers."; return Back(selected); }
            if (selectedBorrowers.Length > 0 && !audience.HasFlag(CalendarAudience.Borrower))
            { TempData["ErrorMessage"] = "Select the Borrowers checkbox to use the specific borrower list."; return Back(selected); }
            if (selectedBorrowers.Length > 0)
            {
                var existing = await _db.Borrowers.CountAsync(b => selectedBorrowers.Contains(b.Id));
                if (existing != selectedBorrowers.Length)
                { TempData["ErrorMessage"] = "One or more selected borrowers no longer exist."; return Back(selected); }
            }
        }
        title = title?.Trim() ?? "";
        description = description?.Trim() ?? "";
        if (title.Length is < 1 or > 120 || description.Length > 800)
        { TempData["ErrorMessage"] = "An event needs a title (up to 120 characters) and up to 800 characters of notes."; return Back(selected); }
        var user = await _users.GetUserAsync(User);
        if (user is null || user.MustChangePassword) return Forbid();
        var entry = new CalendarEntry
        {
            Kind = CalendarEntryKind.Event, Title = title, Description = description,
            ScheduledDate = selected.Date, BorrowerId = creatorBorrowerId, Audience = audience,
            CreatedByUserId = user.Id
        };
        // Empty list = all borrowers when the Borrower role is checked.
        // Other staff roles always see the event if their role was checked.
        foreach (var borrowerId in selectedBorrowers)
            entry.Recipients.Add(new CalendarEntryRecipient { BorrowerId = borrowerId });
        _db.CalendarEntries.Add(entry);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Calendar event created.";
        return Back(selected);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReservation(string date, int itemId, int? borrowerId)
    {
        if (!ParseDate(date, out var selected)) return BadRequest("Invalid calendar date.");
        if (selected.Date < DateTime.Today || selected.Date > DateTime.Today.AddYears(2))
        { TempData["ErrorMessage"] = "Choose a follow-up date between today and two years from now."; return Back(selected); }
        if (User.IsInRole(AppRoles.Borrower))
        {
            borrowerId = await CurrentMemberIdAsync();
            if (!borrowerId.HasValue) return Forbid();
        }
        if (!borrowerId.HasValue || itemId <= 0)
        { TempData["ErrorMessage"] = "Select an item and borrower."; return Back(selected); }
        var user = await _users.GetUserAsync(User);
        if (user is null || user.MustChangePassword) return Forbid();
        var (ok, message) = await _reservations.PlaceAsync(itemId, borrowerId.Value, selected.Date, user.Id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
            ? message + " A follow-up reminder was added for " + selected.ToString("dd MMM yyyy") + "."
            : message;
        return Back(selected);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleNotification(string date, string title, string message,
        CalendarDelivery delivery, int? borrowerId)
    {
        if (!ParseDate(date, out var selected)) return BadRequest("Invalid calendar date.");
        if (selected.Date < DateTime.Today || selected.Date > DateTime.Today.AddYears(2))
        { TempData["ErrorMessage"] = "Select a notification date within the next two years."; return Back(selected); }
        if (User.IsInRole(AppRoles.Borrower))
        {
            borrowerId = await CurrentMemberIdAsync();
            if (!borrowerId.HasValue) return Forbid();
        }
        if (!borrowerId.HasValue || !Enum.IsDefined(delivery))
        { TempData["ErrorMessage"] = "Choose a borrower and delivery method."; return Back(selected); }
        var borrower = await _db.Borrowers.AsNoTracking().SingleOrDefaultAsync(b => b.Id == borrowerId.Value);
        if (borrower is null)
        { TempData["ErrorMessage"] = "Borrower not found."; return Back(selected); }
        if ((delivery != CalendarDelivery.Sms && string.IsNullOrWhiteSpace(borrower.Email)) ||
            (delivery != CalendarDelivery.Email && string.IsNullOrWhiteSpace(borrower.Phone)))
        { TempData["ErrorMessage"] = "The selected borrower is missing contact details for that channel."; return Back(selected); }
        title = title?.Trim() ?? "";
        message = message?.Trim() ?? "";
        if (title.Length is < 1 or > 120 || message.Length is < 1 or > 800)
        { TempData["ErrorMessage"] = "Provide a title (maximum 120 characters) and message (maximum 800 characters)."; return Back(selected); }
        var user = await _users.GetUserAsync(User);
        if (user is null || user.MustChangePassword) return Forbid();
        _db.CalendarEntries.Add(new CalendarEntry
        {
            Kind = CalendarEntryKind.Notification, ScheduledDate = selected.Date,
            Title = title, Description = message, BorrowerId = borrower.Id,
            Delivery = delivery, CreatedByUserId = user.Id
        });
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Notification scheduled. It will appear in the simulator log when the scheduled check runs.";
        return Back(selected);
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Reception + "," + AppRoles.Manager)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RunChecks()
    {
        try
        {
            var checkedCount = await _sweep.RunAsync();
            TempData["SuccessMessage"] = $"Processed {checkedCount} due/overdue loans, holds and scheduled notifications. See the calendar and log.";
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "The sweep was already running; refresh the log to see new notifications.";
        }
        return RedirectToAction(nameof(Index));
    }
}
