# Book Reader AI — Architecture

This document describes the **as-built** architecture of Book Reader AI: how the code is
organized, how requests flow through the layers, the data model, and the cross-cutting
decisions a developer needs to understand before changing things.

For the product vision and code standards see [`AGENTS.md`](../AGENTS.md). For a quick start,
the feature list, and run/test commands see the [README](../README.md). For a contributor-facing
"how do I add X" walkthrough, see [`DEVELOPER_GUIDE.md`](DEVELOPER_GUIDE.md).

---

## 1. Solution layout

The solution is three projects:

| Project | Type | Responsibility |
| --- | --- | --- |
| `BookReaderApp` | ASP.NET MVC web app (.NET 10) | The whole web application: catalog, library, reviews, social, profiles, and the Identity/main `DbContext`. |
| `BookReaderApp.Messaging` | Class library | Self-contained direct-messaging module. Owns its own entities, `DbContext`, repositories, and service. Depends only on EF Core. |
| `BookReaderApp.Tests` | xUnit test project | Unit/data tests for services and repositories. |

`BookReaderApp` references `BookReaderApp.Messaging`; the messaging library references neither
of the others. Everything is wired together in [`BookReaderApp/Program.cs`](../BookReaderApp/Program.cs).

---

## 2. Layered architecture

Within `BookReaderApp` the code follows a strict, one-directional layering:

```
HTTP request
    │
    ▼
Controllers ──▶ Services ──▶ Repositories ──▶ ApplicationDbContext ──▶ SQLite
  (thin)        (business      (IRepository<T>)    (EF Core)
                 logic)
```

**Rules (enforced by convention, see `AGENTS.md`):**

- **Controllers** are thin. They authenticate/authorize, bind input, call a service, map the
  result to a ViewModel, and return a view or redirect. They never touch a repository or the
  `DbContext`. See [`Controllers/BooksController.cs`](../BookReaderApp/Controllers/BooksController.cs)
  for the canonical example.
- **Services** hold all business logic. They depend on repository **interfaces** only — never on
  `DbContext` — which keeps them unit-testable with mocks. One interface + implementation per
  concern (`IBookService`/`BookService`, `IUserBookService`/`UserBookService`, …).
- **Repositories** are the only code that touches EF Core. A generic
  [`IRepository<T>`](../BookReaderApp/Repositories/IRepository.cs) /
  [`EfRepository<T>`](../BookReaderApp/Repositories/EfRepository.cs) pair gives every entity CRUD +
  pagination for free; aggregates that need richer queries get a specialized repository
  (e.g. `IBookRepository`, `IUserBookRepository`) that extends it.
- **Models vs. ViewModels** are kept separate. Domain entities live in `Models/`; request/response
  shapes live in `Models/ViewModels/`.

**Cross-cutting conventions:**

- **Async everywhere.** All I/O is `async`/`await` with `Task<T>` return types and the `Async`
  suffix. `.Result`/`.Wait()` are never used.
- **Pagination.** List queries return the shared
  [`PagedResult<T>`](../BookReaderApp/Models/PagedResult.cs) wrapper (default 20 items/page) rather
  than unbounded sets.

---

## 3. Dependency injection

All wiring lives in `Program.cs`. The notable patterns:

- **Open-generic repository registration** — `AddScoped(typeof(IRepository<>), typeof(EfRepository<>))`
  means every entity has a CRUD repository without an explicit registration.
- **Aliasing a specialized repository to the generic interface** — e.g. `IRepository<Book>` resolves
  to the same instance as `IBookRepository`, so callers can depend on whichever they need:
  ```csharp
  builder.Services.AddScoped<IBookRepository, BookRepository>();
  builder.Services.AddScoped<IRepository<Book>>(sp => sp.GetRequiredService<IBookRepository>());
  ```
- Everything is **scoped** to match the per-request lifetime of the `DbContext` and Identity managers.
- **Constructor injection only** — no service locator.

---

## 4. Data model

### Main context — [`ApplicationDbContext`](../BookReaderApp/Data/ApplicationDbContext.cs)

Extends `IdentityDbContext<ApplicationUser>`, so it owns both the Identity tables and the app domain.

```
ApplicationUser (Identity)
 ├─< UserBook >─ Book ─> Author
 │                 └──> Genre
 ├─< Shelf            (custom shelves)
 ├─< Review >─ Book
 │     ├─< ReviewLike
 │     └─< ReviewComment
 ├─< FriendRequest (Requester / Addressee)
 ├─< Follow         (Follower / Followee)
 └─< AuthorFollow >─ Author
```

Key entities:

| Entity | Notes |
| --- | --- |
| [`ApplicationUser`](../BookReaderApp/Models/ApplicationUser.cs) | Identity user + profile fields (`DisplayName`, `Bio`, `FavoriteGenre`, `ReadingGoal`, `ProfilePicturePath`). |
| [`Book`](../BookReaderApp/Models/Book.cs) | Catalog entry. Belongs to one `Author`, optionally one `Genre`. |
| [`Author`](../BookReaderApp/Models/Author.cs), `Genre` | Shared catalog lookups. |
| [`UserBook`](../BookReaderApp/Models/UserBook.cs) | A book on a user's shelf — the user↔book join. Carries the shelf placement and the user's rating. |
| [`Shelf`](../BookReaderApp/Models/Shelf.cs) | A user-created custom shelf. |
| [`Review`](../BookReaderApp/Models/Review.cs), `ReviewLike`, `ReviewComment` | Reviews and their social interactions. |
| `FriendRequest`, `Follow`, `AuthorFollow` | Social graph: friendships, reader-follows-reader, reader-follows-author. |

**Modeling decisions worth knowing:**

- **One shelf per book.** A `UserBook` sits on *either* a built-in `ReadingStatus`
  (`WantToRead`/`Reading`/`Finished`) *or* a custom `Shelf` — exactly one of `Status` / `ShelfId`
  is set. Ratings are independent of shelf placement.
- **Unique indexes as business rules.** A user can shelve a book once (`UserId+BookId`), review a
  book once (`UserId+BookId`), like a review once, have one friend-request per ordered pair, etc.
- **Delete behavior.** Edges that fan out from a user use `DeleteBehavior.Restrict` on the user side
  to avoid multiple cascade paths into `AspNetUsers` (which SQL Server rejects); the comments in
  `OnModelCreating` flag each case.

### Messaging context — [`MessagingDbContext`](../BookReaderApp.Messaging/Data/MessagingDbContext.cs)

Owns `Conversation` and `DirectMessage`. A `Conversation` is a normalized pair
(`User1Id` ≤ `User2Id`, unique index) so the same two users always map to one thread regardless of
who started it. `DirectMessage.SenderId` is a plain user-id string — the library deliberately has
**no FK or navigation to `ApplicationUser`** so it stays standalone.

The messaging context **shares the physical SQLite file** but uses a separate migrations-history
table (`__EFMigrationsHistory_Messaging`) so the two contexts never collide on schema or migrations.

---

## 5. The messaging module (and how it stays decoupled)

`BookReaderApp.Messaging` knows nothing about the web app or Identity. It declares two
**abstractions** it needs the host to satisfy:

- [`IFriendshipChecker`](../BookReaderApp.Messaging/Abstractions/IFriendshipChecker.cs) — "are these
  two users friends?" (messaging is friends-only).
- [`IMessageNotifier`](../BookReaderApp.Messaging/Abstractions/IMessageNotifier.cs) — "push this new
  message / unread count to a user."

The host supplies both via **adapters** in `BookReaderApp/Adapters/`:

- `FriendRequestFriendshipChecker` delegates to the existing friend-request service.
- `SignalRMessageNotifier` pushes over the [`ChatHub`](../BookReaderApp/Hubs/ChatHub.cs).

This keeps real-time transport (SignalR) and the social graph as host concerns while the messaging
logic stays a pure, testable library.

### Real-time delivery

[`ChatHub`](../BookReaderApp/Hubs/ChatHub.cs) is a **server→client push-only** hub (clients never
invoke it), mapped at `/hubs/chat`. SignalR keys connections by the `NameIdentifier` claim (the
Identity user id), so `Clients.User(userId)` reaches exactly that person's open tabs. The hub raises
two events: `MessageReceived` and `UnreadCountChanged`.

---

## 6. Security

Configured in `Program.cs` and aligned with the policy in `AGENTS.md`:

- **Identity** with a strong password policy (≥8 chars, upper/lower/digit/non-alphanumeric), unique
  email required.
- **Lockout** — 5 failed attempts → 15-minute lockout.
- **Cookies** — `HttpOnly`, `SecurePolicy.Always`, 30-minute sliding expiration.
- **HTTPS** enforced (`UseHttpsRedirection`, HSTS outside Development).
- **CSRF** — `AutoValidateAntiforgeryTokenAttribute` is registered globally, so every unsafe verb
  (POST/PUT/DELETE) is antiforgery-validated without per-action annotations.
- **Authorization** — role-based via [`AppRoles`](../BookReaderApp/Models/AppRoles.cs)
  (`User`/`Moderator`/`Admin`). Catalog mutations require `Admin`/`Moderator`; personal/social
  actions require an authenticated user.
- **Secrets** — the Google Books API key is read from an untracked `appsettings.Local.json` overlay
  and attached server-side via a typed `HttpClient`; it never reaches the browser.

---

## 7. Startup sequence

On boot, `Program.cs`:

1. Builds configuration (including the optional `appsettings.Local.json` overlay).
2. Registers the two `DbContext`s, Identity, repositories, services, the messaging module, Google
   Books `HttpClient`, global antiforgery, and SignalR.
3. Configures the HTTP pipeline (exception handler + HSTS in prod, HTTPS redirect, auth, static
   assets, default MVC route, `ChatHub` at `/hubs/chat`).
4. Runs [`DbInitializer.SeedAsync`](../BookReaderApp/Data/DbInitializer.cs) — migrates the main
   context, seeds roles and the admin/test users, and seeds a sample book + review.
5. Migrates the messaging context.

Because migrations run at startup, a fresh clone comes up with a ready database and seed data;
the explicit `dotnet ef database update` step is optional.

---

## 8. Deviations from the original plan

The earlier version of this document (and parts of `AGENTS.md`) described an aspirational design.
The shipped system differs in a few ways worth flagging so they don't surprise you:

- **Database is SQLite** (`bookreader.db`), not SQL Server.
- **Book Clubs**, **Discovery/Recommendations**, and a dedicated **Infrastructure/** layer are *not*
  implemented. Social features shipped instead as friends/follows/messaging.
- There is **no separate REST/JSON API surface**; AJAX endpoints (Google Books search, batch
  create) return JSON from regular MVC controllers.
