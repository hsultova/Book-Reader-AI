# Book Reader AI — Technological Module Breakdown

This document decomposes the Book Reader application (see `AGENTS.md` for the full vision) into technological modules. It defines the boundaries that future features must land in. Nothing here is implemented yet — the project has not been scaffolded.

The system splits into **6 functional (vertical) modules** layered on **4 technical (horizontal) modules**, all inside a single ASP.NET MVC solution (`BookReaderApp`) with a companion test project (`BookReaderApp.Tests`).

---

## Functional Modules (Vertical Slices)

Each slice owns its controllers, services, repositories, domain models, view models, and Razor views.

### 1. Identity & Access
- User registration, login, and profiles via ASP.NET Core Identity.
- Role-based authorization: User, Moderator, Admin.
- Session management (30-minute inactivity timeout), login rate limiting (5 attempts, 15-minute lockout), CSRF enforcement.
- Key types: `ApplicationUser`, `AccountController`, `ProfileController`.

### 2. Book Catalog
- Canonical book/author/genre data shared by all other modules.
- Book metadata (title, author, ISBN, cover image, description, genre), search and filtering.
- Cover image upload (50MB file limit) and storage.
- Key types: `Book`, `Author`, `Genre`, `BookService`, `BooksController`.

### 3. Personal Library & Reading Progress
- The "digital bookshelf": each user's collection of catalog books.
- Reading status (Want to Read / Reading / Finished), progress tracking (page/percent), reading journal entries.
- Reading statistics (books per year, pages read).
- Key types: `LibraryEntry`, `ReadingProgress`, `JournalEntry`, `LibraryService`, `LibraryController`.

### 4. Reviews & Ratings
- Star ratings and written reviews on catalog books; reading notes.
- Aggregate rating computation per book; moderation hooks (Moderator role).
- Key types: `Review`, `Rating`, `ReadingNote`, `ReviewService`, `ReviewsController`.

### 5. Book Clubs & Community
- Clubs with membership, club reading lists, and discussion threads.
- Reader-to-reader and reader-to-author connections (follow).
- Key types: `BookClub`, `ClubMembership`, `DiscussionThread`, `DiscussionPost`, `ClubService`, `ClubsController`.

### 6. Discovery & Recommendations
- Popular/trending books (cached 15–30 minutes per the caching strategy in `AGENTS.md`).
- Personalized recommendations from library + ratings data; community-review-driven discovery feeds.
- Read-only consumer of modules 2–5; a natural seam for future AI features.
- Key types: `RecommendationService`, `DiscoverController`.

---

## Technical Modules (Horizontal Layers)

### 7. Web & API Layer (`Controllers/`, `Views/`, `wwwroot/`)
- MVC controllers returning Razor views (Bootstrap, mobile-first) plus RESTful JSON endpoints for AJAX and mobile clients.
- Thin controllers: validate input, delegate to services, map to view models, return proper HTTP status codes.
- Shared layout, partials, bundled/minified static assets.

### 8. Service Layer (`Services/`)
- All business logic; one service interface + implementation per functional module (`IBookService`/`BookService`, etc.).
- Fully async (`Async` suffix, `Task<T>` return types, never `.Result`/`.Wait()`).
- Consumes repositories only — never `DbContext` directly.

### 9. Data Access Layer (`Data/`, `Repositories/`)
- EF Core `DbContext`, entity configurations, migrations (SQL Server).
- Generic `IRepository<T>` plus per-aggregate repositories.
- Shared `PagedResult<T>` wrapper — every list query paginated, default 20 per page; `.AsNoTracking()` for reads; indexes on `UserId`, `BookId`, `CreatedDate`.

### 10. Infrastructure & Cross-Cutting (`Infrastructure/`)
- Caching (in-memory, write-through invalidation), structured logging (Serilog-style), global error handling middleware.
- Input sanitization helpers, rate limiting, file upload handling, configuration/secrets access.
- Used by all layers via DI (constructor injection, registered in `Program.cs`).

---

## Testing (`BookReaderApp.Tests`)

- `Tests/Services/` — unit tests per service with mocked repositories (80% coverage target).
- `Tests/Integration/` — service + repository + in-memory database tests.
- Naming convention: `Method_Scenario_ExpectedBehavior`.

---

## Dependency Rules

```
Web/API → Services → Repositories → DbContext
            ↓
      Infrastructure (cross-cutting, available to all)
```

- Functional modules depend on **Book Catalog** (the shared core) but not on each other, with one exception: **Discovery** reads from Library, Reviews, and Clubs.
- No layer skipping: controllers never touch repositories or `DbContext`.
