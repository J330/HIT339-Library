using S379658_HIT339_Assignment2_Library.Models;
namespace S379658_HIT339_Assignment2_Library.Services;
public sealed record CheckoutResult(bool Succeeded, string Message);
public sealed class KioskCheckoutService
{
    private readonly LibraryCheckoutService _checkout;
    public KioskCheckoutService(LibraryCheckoutService checkout) => _checkout = checkout;
    public Task<CheckoutResult> CheckoutAsync(int borrowerId, string libraryCode)
        => _checkout.CheckoutAsync(borrowerId, libraryCode, DateTime.Today, DateTime.Today.AddDays(14), channel: CheckoutChannel.Kiosk);
}
