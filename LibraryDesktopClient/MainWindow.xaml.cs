using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace LibraryDesktopClient;

public partial class MainWindow : Window
{
    private Uri? _libraryUri;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += Window_Loaded;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var settingsFile = Path.Combine(AppContext.BaseDirectory, "library-client.json");
            using var settings = JsonDocument.Parse(await File.ReadAllTextAsync(settingsFile));
            var url = settings.RootElement.GetProperty("LibraryUrl").GetString();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var site) ||
                (site.Scheme != Uri.UriSchemeHttps &&
                 !(site.Scheme == Uri.UriSchemeHttp && site.IsLoopback)))
            {
                throw new InvalidOperationException("LibraryUrl must be an HTTPS address (HTTP localhost is only for local testing).");
            }
            _libraryUri = site;

            // Keep browser-profile data in the current Windows user's application data.
            var profileFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LibraryDesktopClient", "BrowserData");
            Directory.CreateDirectory(profileFolder);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profileFolder);
            await Browser.EnsureCoreWebView2Async(environment);
            Browser.CoreWebView2.NavigationStarting += (_, args) =>
            {
                if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var target) ||
                    target.Scheme != site.Scheme || target.Host != site.Host || target.Port != site.Port)
                    args.Cancel = true;
            };
            Browser.CoreWebView2.NewWindowRequested += (_, args) =>
            {
                args.Handled = true; // Popups cannot bypass the same-origin navigation rule.
                if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var target) &&
                    target.Scheme == site.Scheme && target.Host == site.Host && target.Port == site.Port)
                    Browser.CoreWebView2.Navigate(args.Uri);
            };
            Browser.CoreWebView2.Navigate(_libraryUri.ToString());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to connect to the library: {ex.Message}",
                "Library client setup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        if (_libraryUri is not null && Browser.CoreWebView2 is not null)
            Browser.CoreWebView2.Navigate(_libraryUri.ToString());
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (Browser.CanGoBack) Browser.GoBack();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (Browser.CoreWebView2 is not null) Browser.CoreWebView2.Reload();
    }
}
