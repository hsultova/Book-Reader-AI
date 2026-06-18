# Developer Guide

A practical guide for working in the Book Reader AI codebase: where things live, how to add a
feature without fighting the conventions, the route reference, and the test approach.

Read [`docs/ARCHITECTURE.md`](ARCHITECTURE.md) first for the big picture. This guide is the
day-to-day companion.

---

## Environment setup

| Need | Get it |
| --- | --- |
| .NET 10 SDK | <https://dotnet.microsoft.com/download> |
| Google Books API key (optional) | <https://developers.google.com/books/docs/v1/using#APIKey> — only needed to search/import books on the Create page |

```powershell
dotnet restore
dotnet run --project BookReaderApp
```

The app serves on `https://localhost:7009` and `http://localhost:5235`. The SQLite database is
created, migrated, and seeded on first run — no manual DB step required.

**Seeded accounts** (dev only; passwords come from `Seed:*` config when set):

| Email | Role | Default password |
| --- | --- | --- |
| `admin@bookreader.local` | Admin | `Admin#12345` |
| `user@bookreader.local` | User | `User#12345` |

**Local secrets:** put the Google Books key in an untracked `BookReaderApp/appsettings.Local.json`:

```json
{ "GoogleBooks": { "ApiKey": "YOUR_KEY" } }
```

---

## Where things live

```
BookReaderApp/
├── Controllers/        # One per feature area; thin
├── Services/           # I<Name>Service + <Name>Service pairs; *Results.cs hold result records
├── Repositories/       # IRepository<T>/EfRepository<T> + specialized I<Name>Repository pairs
├── Models/             # Domain entities + enums
│   └── ViewModels/     # Request/response shapes for views
├── Data/               # ApplicationDbContext, DbInitializer (seeding)
├── Hubs/               # ChatHub (SignalR, server→client push)
├── Adapters/           # Host implementations of the messaging module's abstractions
├── Configuration/      # Strongly-typed options (GoogleBooksOptions)
├── Migrations/         # EF Core migrations for the main context
├── Views/              # Razor views, grouped by controller; Shared/ for layout & partials
└── wwwroot/            # Static assets

BookReaderApp.Messaging/ # Standalone messaging library (own DbContext + migrations)
BookReaderApp.Tests/     # xUnit tests
```

**Naming conventions** (full list in `AGENTS.md`):

- PascalCase types/methods/properties; `_camelCase` private fields; `UPPER_SNAKE_CASE` constants.
- Async methods end in `Async` and return `Task`/`Task<T>`.
- Tests: `Method_Scenario_ExpectedBehavior`.
- Comments explain **why**, not **what**.

---

## How to add a feature (end-to-end)

Follow the dependency direction — build inward-out, from the entity to the view. Adding a "book
of the month" feature would look like:

**1. Model.** Add the entity to `Models/` and a `DbSet<>` + any relationships/indexes in
[`ApplicationDbContext.OnModelCreating`](../BookReaderApp/Data/ApplicationDbContext.cs). Use a
unique index when "at most one per user/pair" is a rule (see how `UserBook`, `Review`,
`FriendRequest` do it).

**2. Migration.**
```powershell
dotnet ef migrations add AddBookOfTheMonth --project BookReaderApp
# applied automatically on next run, or: dotnet ef database update --project BookReaderApp
```

**3. Repository.** If generic CRUD (`GetByIdAsync`, `GetPagedAsync`, `AddAsync`, …) is enough, just
inject `IRepository<YourEntity>` — it's already registered open-generically. If you need richer
queries, add `IYourRepository : IRepository<YourEntity>` + an implementation extending
`EfRepository<YourEntity>`, then register it in `Program.cs` (and alias `IRepository<YourEntity>`
to it if both are used).

**4. Service.** Add `IYourService` + `YourService` with the business logic. Depend on repository
**interfaces** only. For operations with multiple failure modes, return a result record (see
[`AccountResults.cs`](../BookReaderApp/Services/AccountResults.cs),
[`BookResults.cs`](../BookReaderApp/Services/BookResults.cs)) rather than throwing for control flow.
Register the service in `Program.cs` as `AddScoped`.

**5. ViewModel + Controller + View.** Add a ViewModel in `Models/ViewModels/`, a thin controller
action that calls the service and maps to it, and a Razor view under `Views/<Controller>/`. Apply
`[Authorize]` / `[Authorize(Roles = ...)]` as needed (CSRF is already global).

**6. Tests.** Add a `*Tests` class under `BookReaderApp.Tests` mocking the repositories with Moq;
use the EF Core InMemory provider when you need a real context.

---

## Route reference

MVC convention routing: `/{controller}/{action}/{id?}`, default `Home/Index`. "Auth" = requires
login; "Admin/Mod" = requires the `Admin` or `Moderator` role; "Public" = anonymous allowed.

### Catalog — `BooksController`
| Verb | Route | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/Books?q=&page=` | Public | Search/browse catalog (paged) |
| GET | `/Books/Details/{id}` | Public | Book detail + reviews/likes/comments |
| GET/POST | `/Books/Create` | Admin/Mod | Create a book |
| GET | `/Books/SearchGoogleBooks?query=` | Admin/Mod | JSON proxy to Google Books (key stays server-side) |
| POST | `/Books/CreateBatch` | Admin/Mod | Bulk-create selected Google Books results (JSON body) |
| GET/POST | `/Books/Edit/{id}` | Admin/Mod | Edit a book |
| GET/POST | `/Books/Delete/{id}` | Admin/Mod | Delete a book |

### Personal library — `MyBooksController` (all Auth)
| Verb | Route | Purpose |
| --- | --- | --- |
| GET | `/MyBooks?status=&shelfId=&q=` | The user's shelf, filterable by status/shelf/search |
| POST | `/MyBooks/SetStatus` | Shelve/move a book to a built-in status |
| POST | `/MyBooks/SetShelf` | Move a book to a custom shelf |
| POST | `/MyBooks/CreateShelf` | Create a shelf and place a book on it |
| POST | `/MyBooks/DeleteShelf` | Delete a custom shelf |
| POST | `/MyBooks/SetRating` | Set/clear the user's 1–5 rating |
| POST | `/MyBooks/Remove` | Remove a book from the shelf |

### Reviews — `ReviewsController` (all Auth)
| Verb | Route | Purpose |
| --- | --- | --- |
| POST | `/Reviews/Save` | Create/update the user's review of a book |
| POST | `/Reviews/Delete` | Delete the user's review |
| POST | `/Reviews/ToggleLike` | Like/unlike a review (self-likes ignored) |
| POST | `/Reviews/AddComment` | Comment on a review |
| POST | `/Reviews/DeleteComment` | Delete a comment |

### Authors — `AuthorsController`
| Verb | Route | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/Authors/Details/{id}` | Public | Author page + books + follower count |
| POST | `/Authors/Follow/{id}` · `/Authors/Unfollow/{id}` | Auth | Follow/unfollow an author |

### Social — `FriendsController`, `FollowController` (all Auth)
| Verb | Route | Purpose |
| --- | --- | --- |
| GET | `/Friends?q=` | Friends, requests, and people search |
| POST | `/Friends/Send` `/Accept` `/Reject` `/Cancel` `/Unfriend` | Friend-request lifecycle |
| POST | `/Follow/Follow/{id}` · `/Follow/Unfollow/{id}` | Follow/unfollow a reader |

### Messaging — `MessagesController` (all Auth)
| Verb | Route | Purpose |
| --- | --- | --- |
| GET | `/Messages` | Conversation list |
| POST | `/Messages/Open/{id}` | Open/create a conversation with a user |
| GET | `/Messages/Thread/{id}` | A conversation thread |
| POST | `/Messages/Send` | Send a message (friends-only, real-time push) |
| POST | `/Messages/MarkRead/{id}` | Mark a conversation read |
| — | `/hubs/chat` | SignalR hub (server→client push) |

### Profile & account
| Verb | Route | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/Profile/Index/{id?}` | Auth | View a profile (own if no id) |
| GET/POST | `/Profile/Settings` | Auth | Edit own profile |
| GET/POST | `/Account/Register` · `/Account/Login` | Public | Auth flows |
| POST | `/Account/Logout` | Auth | Sign out |
| GET | `/Account/AccessDenied` | Public | 403 page |
| GET | `/` | Public | Home / activity updates |

---

## Testing

```powershell
dotnet test                                                            # all
dotnet test --filter "FullyQualifiedName~BookReaderApp.Tests.Services.BookServiceTests"   # one class
dotnet test --filter "FullyQualifiedName=...BookServiceTests.GetBookById_WithValidId_ReturnsBook"  # one test
```

- **Services** are tested with Moq'd repositories — the layering is what makes this clean.
- **Data/repository** tests use the EF Core InMemory provider.
- Name tests `Method_Scenario_ExpectedBehavior`.
- `DbInitializer` is split into a host entry point and a manager-facing core specifically so seeding
  can be tested against an in-memory store without a web host.

---

## Common gotchas

- **Don't inject `DbContext` into a service or controller** — go through a repository. Tests and the
  layering rules both depend on it.
- **One shelf per book.** Setting a status clears the custom-shelf placement and vice versa; don't
  set both `Status` and `ShelfId` on a `UserBook`.
- **CSRF is global.** Every POST is antiforgery-validated; make sure forms render the token (the
  Razor form tag helpers do this automatically). For JSON POSTs, send the token header (see
  `CreateBatch`).
- **Two migration sets.** Schema changes to messaging entities use the messaging project's own
  context: `dotnet ef migrations add <Name> --project BookReaderApp.Messaging`. Everything else
  uses `BookReaderApp`.
- **The Google Books key never goes to the browser.** Always proxy through `SearchGoogleBooks`.
