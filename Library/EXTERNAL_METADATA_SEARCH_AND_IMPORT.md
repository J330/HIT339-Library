# Public search + Open Library / MusicBrainz / mock importer

This change extends the latest full seven-deliverable .NET 10 MVC project (including the calendar, standard kiosk header and member usernames). **No migration is required**: all imports use the existing Book, Music, Toy, Item and LibraryBranch models.

## Where to find the features

| URL | Access | Function |
| --- | --- | --- |
| `/Catalogue` | Everyone | Search *locally held library items* by title, author, artist, genre/category, item type, availability and branch. This does not search the external services. |
| `/api/catalogue/items?search=tolkien&branchId=1` | Everyone | Read-only public JSON catalogue; searched fields include author, artist and genre. Public rate limits still apply. |
| `/Imports/Search` | Admin, Manager, Reception | Search Open Library books by keyword/title/author/ISBN; MusicBrainz release groups by album/artist; or offline mock samples. Maximum 12 results per request. |
| `/Imports/Preview?provider=OpenLibrary&externalId=OL...W` | Staff only | Reload source metadata and review fields; choose an active branch and *new unique physical-copy code*. |
| `/Imports/Index` | Staff only | Original CSV upload and mock bulk import tools (unchanged). |

## How to run

1. Extract ZIP. Open `Library/S379658-HIT339-Assignment2-Library.slnx` in Visual Studio with .NET 10.
2. Verify `appsettings.json` database connection and **replace** the sample `Metadata:MusicBrainzUserAgent` contact address with an actual address or website under your control. MusicBrainz requires an identifying User-Agent.
3. Run `dotnet restore`, `dotnet build`, then `dotnet run`. If database migrations have *already* been applied for the previous version, **do not add a new migration** for this change. If they haven't, back up your data and apply `Update-Database` in the Visual Studio Package Manager Console.
4. Log in as Admin, Manager or Reception, choose **Search external media** from the relevant navigation menu.
5. Example: provider `OpenLibrary`, `Book title`, search `The Hobbit`. Choose **Review & import**, check title and author, enter unique code (e.g. `MAIN-B-0199`), select a branch, then **Confirm import**. Search `/Catalogue` for the saved title.
6. Example: provider `MusicBrainz`, `Album name`, search `Abbey Road`. Review the release year (enter one if not available), genre and artist; import with another unique copy code.
7. Offline example: provider `Mock`, search `astronomy`, preview the sample, and import to an active branch. CSV/mock *bulk* importing is still available from `/Imports`.

## Safeguards / expectations

- External search results are only metadata. **Search does not modify inventory.** A new physical-copy record is created only after staff confirmation with an active branch and unique code. Titles can have multiple copies; ISBN or MusicBrainz ID is not stored in the existing schema.
- Preview and confirmation fetch metadata on the server; posted titles and type fields cannot replace the selected source. Connections can fail or time out; the importer displays an error and leaves the database unchanged. HTTP connections use fixed external hosts, not arbitrary user URLs.
- Public catalogue search is anonymous; **external importing is only accessible to Admin / Manager / Reception**. The existing read-only catalogue API does not expose borrowers, payment details or reservations.
- MusicBrainz access is serialized with at least 1.1 seconds between requests per application process and identifies itself using the configured User-Agent. If running multiple application instances, configure shared rate limiting appropriately. External APIs may have other fair-use limits.
- The mock record search can suggest toys as well as books/music; Open Library searches books, MusicBrainz searches music. The normal CSV importer supports all three types.
- Missing metadata (especially book author or music year) can be entered by staff during review. Data is limited to existing model fields. Book ISBN, publisher, cover art, and provider IDs are not persisted because the original Book model does not have those columns.
- Uploaded CSV is parsed without saving the uploaded file; original duplicate-code validation remains in effect. Importing a second physical copy requires a second unique code.

## Acceptance checks

1. Anonymous user: `/Catalogue?searchString=Jazz` works; `/Imports/Search` redirects to sign-in/access denied.
2. Admin/Reception/Manager: `/Imports/Search?provider=Mock&searchType=keyword&query=astronomy` returns a row; click preview, select MAIN, assign new code; confirm creates a single Book.
3. Repeat import with same code: fails validation; no new copy.
4. Search `/Catalogue?searchString=Sample+Author` and `/api/catalogue/items?search=Sample%20Author`: imported item appears (also searchable by author).
5. Filter by branch + genre; results belong only to that branch/category.
6. Disconnect from network: Open Library/MusicBrainz search shows a warning; mock search still works.
7. Test MusicBrainz album and artist searches. Confirm with a missing release year should require staff to supply the year.
8. Anonymous and Borrower roles cannot POST `/Imports/Confirm` or `/Imports/Csv`.
9. Verify original Reservations, Kiosk, Branches, Notifications Calendar, Reports and member usernames are still accessible.

## Main code changes

- `Services/ExternalMetadataService.cs`: HTTP API adapters and offline mock search.
- `ViewModels/ImportViewModels.cs`: search, preview/confirmation models.
- `Controllers/ImportsController.cs`: staff-authorized search, preview and anti-forgery-protected confirm.
- `Services/MediaImportService.cs`: external import using the same inventory constraints.
- `Views/Imports/Search.cshtml`, `Preview.cshtml`, `Index.cshtml`: interface.
- `Controllers/CatalogueController.cs`, `CatalogueApiController.cs`, `Views/Catalogue/Index.cshtml`: public library catalogue search filters.
- `Views/Shared/_Layout.cshtml`, `Views/Developers/Index.cshtml`, `Program.cs`, `appsettings.json`: navigation, documentation and HTTP client setup.

## Primary references

- [Open Library Search API](https://openlibrary.org/dev/docs/api/search) — `/search.json` query parameters and response shape.
- [Open Library Books & Works APIs](https://openlibrary.org/dev/docs/api/books) — `/works/{id}.json` and author records.
- [MusicBrainz Search API](https://musicbrainz.org/doc/MusicBrainz_API/Search) and [release-group queries](https://musicbrainz.org/doc/MusicBrainz_API/Search/ReleaseGroupSearch).
- [MusicBrainz API rate limit and User-Agent](https://musicbrainz.org/doc/MusicBrainz_API) — no more than one request per second per application.
- [Microsoft: IHttpClientFactory](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests) — typed client configuration.
- [Microsoft: Authorization by roles](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles).

**Testing status:** Source reviewed and ZIP checked, but the .NET SDK and SQL Server are unavailable in this environment; compilation and live API/database integration must be validated on the Windows development machine.
