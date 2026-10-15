using System.Text.Json;
using System.Text.RegularExpressions;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Services;

// Calls only fixed, trusted metadata hosts. Search results are untrusted suggestions;
// selected records are fetched again on the server before being saved to inventory.
public sealed class ExternalMetadataService
{
    private readonly HttpClient _http;
    private readonly IWebHostEnvironment _environment;
    private readonly string _userAgent;
    private static readonly SemaphoreSlim MusicBrainzGate = new(1, 1);
    private static DateTimeOffset _lastMusicBrainzRequest = DateTimeOffset.MinValue;

    public ExternalMetadataService(HttpClient http, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _http = http;
        _environment = environment;
        _userAgent = configuration["Metadata:MusicBrainzUserAgent"]
            ?? "LibraryCourseProject/1.0 (educational-demo; contact: admin@example.com)";
    }

    public async Task<List<ExternalMediaRecord>> SearchAsync(string provider, string mode, string query, CancellationToken token)
    {
        query = (query ?? "").Trim();
        if (query.Length < 2 || query.Length > 100)
            throw new ArgumentException("Enter a search term between 2 and 100 characters.");
        if (provider == "Mock") return await SearchMockAsync(query, token);
        if (provider == "OpenLibrary")
        {
            var field = mode switch
            {
                "title" => "title", "author" => "author", "isbn" => "isbn", "keyword" => "q",
                _ => throw new ArgumentException("Choose a valid book search type.")
            };
            var url = $"https://openlibrary.org/search.json?{field}={Uri.EscapeDataString(query)}&limit=12&fields=key,title,author_name,first_publish_year,subject";
            using var json = await GetJsonAsync(url, false, token);
            var results = new List<ExternalMediaRecord>();
            if (!TryProperty(json.RootElement, "docs", out var docs) || docs.ValueKind != JsonValueKind.Array) return results;
            foreach (var item in docs.EnumerateArray())
            {
                var key = Text(item, "key");
                var match = Regex.Match(key, @"^/works/(OL\d+W)$", RegexOptions.CultureInvariant);
                if (!match.Success) continue;
                var title = Text(item, "title");
                if (string.IsNullOrWhiteSpace(title)) continue;
                results.Add(new ExternalMediaRecord
                {
                    Provider = "OpenLibrary", ExternalId = match.Groups[1].Value, Type = "Book",
                    Title = Short(title, 150), Creator = Short(FirstString(item, "author_name") ?? "", 150),
                    Genre = Short(FirstString(item, "subject") ?? "General", 100),
                    Year = Number(item, "first_publish_year"),
                    SourceUrl = $"https://openlibrary.org/works/{match.Groups[1].Value}"
                });
            }
            return results;
        }
        if (provider == "MusicBrainz")
        {
            var field = mode switch
            {
                "album" => "releasegroup", "artist" => "artist", "keyword" => "releasegroup",
                _ => throw new ArgumentException("Choose a valid music search type.")
            };
            // Release-group search works for albums, rather than individual recording duplicates.
            var luceneTerm = $"{field}:{QuoteLucene(query)}";
            var url = $"https://musicbrainz.org/ws/2/release-group/?query={Uri.EscapeDataString(luceneTerm)}&fmt=json&limit=12";
            using var json = await GetJsonAsync(url, true, token);
            var results = new List<ExternalMediaRecord>();
            if (!TryProperty(json.RootElement, "release-groups", out var groups) || groups.ValueKind != JsonValueKind.Array) return results;
            foreach (var item in groups.EnumerateArray())
            {
                var id = Text(item, "id");
                if (!Guid.TryParse(id, out _)) continue;
                var title = Text(item, "title");
                if (string.IsNullOrWhiteSpace(title)) continue;
                results.Add(new ExternalMediaRecord
                {
                    Provider = "MusicBrainz", ExternalId = id, Type = "Music",
                    Title = Short(title, 150), Creator = Short(ArtistName(item), 150),
                    Genre = "Music", Year = YearFromDate(Text(item, "first-release-date")),
                    SourceUrl = $"https://musicbrainz.org/release-group/{id}"
                });
            }
            return results;
        }
        throw new ArgumentException("Unknown metadata provider.");
    }

    public async Task<ExternalMediaRecord?> GetByIdAsync(string provider, string id, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 80) return null;
        if (provider == "Mock")
        {
            var rows = await MockRowsAsync(token);
            var row = rows.FirstOrDefault(r => string.Equals(r.LibraryCode, id, StringComparison.OrdinalIgnoreCase));
            return row is null ? null : FromMock(row);
        }
        if (provider == "OpenLibrary")
        {
            if (!Regex.IsMatch(id, @"^OL\d+W$", RegexOptions.CultureInvariant)) return null;
            using var json = await GetJsonAsync($"https://openlibrary.org/works/{id}.json", false, token);
            var root = json.RootElement;
            var title = Text(root, "title");
            if (string.IsNullOrWhiteSpace(title)) return null;
            var authorName = "";
            if (TryProperty(root, "authors", out var authors) && authors.ValueKind == JsonValueKind.Array)
            {
                foreach (var author in authors.EnumerateArray().Take(1))
                {
                    if (!TryProperty(author, "author", out var a)) continue;
                    var authorKey = Text(a, "key");
                    if (!Regex.IsMatch(authorKey, @"^/authors/OL\d+A$")) continue;
                    // A missing author record should not hide the book: staff can enter its author.
                    try
                    {
                        using var authorJson = await GetJsonAsync($"https://openlibrary.org{authorKey}.json", false, token);
                        authorName = Text(authorJson.RootElement, "name");
                    }
                    catch (HttpRequestException) { }
                    catch (TaskCanceledException) when (!token.IsCancellationRequested) { }
                }
            }
            var description = Text(root, "description");
            if (TryProperty(root, "description", out var d) && d.ValueKind == JsonValueKind.Object)
                description = Text(d, "value");
            return new ExternalMediaRecord
            {
                Provider = provider, ExternalId = id, Type = "Book", Title = Short(title, 150),
                Creator = Short(authorName, 150), Genre = Short(FirstString(root, "subjects") ?? "General", 100),
                Description = Short(description, 1000), SourceUrl = $"https://openlibrary.org/works/{id}"
            };
        }
        if (provider == "MusicBrainz")
        {
            if (!Guid.TryParse(id, out var guid)) return null;
            using var json = await GetJsonAsync($"https://musicbrainz.org/ws/2/release-group/{guid:D}?inc=artist-credits&fmt=json", true, token);
            var root = json.RootElement;
            var title = Text(root, "title");
            if (string.IsNullOrWhiteSpace(title)) return null;
            return new ExternalMediaRecord
            {
                Provider = provider, ExternalId = guid.ToString("D"), Type = "Music", Title = Short(title, 150),
                Creator = Short(ArtistName(root), 150), Genre = "Music",
                Year = YearFromDate(Text(root, "first-release-date")),
                SourceUrl = $"https://musicbrainz.org/release-group/{guid:D}"
            };
        }
        return null;
    }

    private async Task<List<MediaImportRow>> MockRowsAsync(CancellationToken token)
    {
        var file = Path.Combine(_environment.ContentRootPath, "Resources", "mock-media.json");
        await using var input = File.OpenRead(file);
        return await JsonSerializer.DeserializeAsync<List<MediaImportRow>>(input,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, token) ?? [];
    }

    private async Task<List<ExternalMediaRecord>> SearchMockAsync(string query, CancellationToken token) =>
        (await MockRowsAsync(token))
            .Where(r => new[] { r.Name, r.Author, r.Artist, r.Description, r.Genre, r.ToyType }
                .Any(s => (s ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)))
            .Take(12).Select(FromMock).ToList();

    private static ExternalMediaRecord FromMock(MediaImportRow r) => new()
    {
        Provider = "Mock", ExternalId = r.LibraryCode, Type = r.Type,
        Title = Short(r.Name, 150), Creator = Short(r.Type == "Music" ? r.Artist : r.Author, 150),
        Genre = Short(r.Type == "Toy" ? r.ToyType : r.Genre, 100),
        Description = Short(r.Description, 1000),
        Year = int.TryParse(r.Year, out var year) ? year : null,
        MinimumAge = int.TryParse(r.MinimumAge, out var age) ? age : null,
        SourceUrl = ""
    };

    private async Task<JsonDocument> GetJsonAsync(string url, bool musicBrainz, CancellationToken token)
    {
        if (musicBrainz) await MusicBrainzGate.WaitAsync(token);
        try
        {
            if (musicBrainz)
            {
                var remaining = TimeSpan.FromMilliseconds(1100) - (DateTimeOffset.UtcNow - _lastMusicBrainzRequest);
                if (remaining > TimeSpan.Zero) await Task.Delay(remaining, token);
                _lastMusicBrainzRequest = DateTimeOffset.UtcNow;
            }
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > 2_000_000)
                throw new HttpRequestException("Metadata response exceeded the allowed size.");
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            return await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 32 }, token);
        }
        finally { if (musicBrainz) MusicBrainzGate.Release(); }
    }

    private static bool TryProperty(JsonElement obj, string name, out JsonElement result)
    {
        result = default;
        return obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out result);
    }
    private static string Text(JsonElement obj, string name) =>
        TryProperty(obj, name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    private static int? Number(JsonElement obj, string name) =>
        TryProperty(obj, name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var value) ? value : null;
    private static string? FirstString(JsonElement obj, string name) =>
        TryProperty(obj, name, out var v) && v.ValueKind == JsonValueKind.Array
            ? v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()).FirstOrDefault()
            : null;
    private static string ArtistName(JsonElement root)
    {
        if (!TryProperty(root, "artist-credit", out var artists) || artists.ValueKind != JsonValueKind.Array) return "";
        foreach (var a in artists.EnumerateArray())
        {
            var name = Text(a, "name");
            if (!string.IsNullOrWhiteSpace(name)) return name;
            if (TryProperty(a, "artist", out var artist)) return Text(artist, "name");
        }
        return "";
    }
    private static int? YearFromDate(string date) =>
        date.Length >= 4 && int.TryParse(date[..4], out var year) && year >= 1000 && year <= DateTime.UtcNow.Year + 1
            ? year : null;
    private static string Short(string? value, int length)
    {
        var v = (value ?? "").Trim();
        return v.Length > length ? v[..length] : v;
    }
    private static string QuoteLucene(string term) => "\"" + Regex.Replace(term, @"[^\p{L}\p{N} ]", " ").Trim() + "\"";
}

public sealed class ExternalMediaRecord
{
    public string Provider { get; init; } = "";
    public string ExternalId { get; init; } = "";
    public string Type { get; init; } = "";
    public string Title { get; init; } = "";
    public string Creator { get; init; } = "";
    public string Genre { get; init; } = "";
    public string Description { get; init; } = "";
    public int? Year { get; init; }
    public int? MinimumAge { get; init; }
    public string SourceUrl { get; init; } = "";
}
