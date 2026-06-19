# Book Reader AI

A modern ASP.NET MVC web application that serves as a **digital bookshelf, reading journal, and book club community**. Track your reading, organize your library across shelves, write and discuss reviews, connect with other readers, and chat in real time.

Built as the course project for the SoftUni *AI-Assisted Development* course.

---

## Features

- **Personal library & shelves** — add books to your collection and organize them across custom shelves.
- **Reading journal** — track reading status (e.g. *Want to Read*, *Reading*, *Read*) per book.
- **Book discovery** — search and import titles via the [Google Books API](https://developers.google.com/books).
- **Reviews & ratings** — write reviews, like them, and comment in threaded discussions, with aggregate rating summaries per book.
- **Social** — follow other readers, send and manage friend requests, and view activity updates.
- **Direct messaging** — real-time one-to-one chat powered by SignalR, isolated in its own messaging module.
- **Authentication & security** — ASP.NET Core Identity with a strong password policy, account lockout, enforced HTTPS, and antiforgery (CSRF) protection on every unsafe request.

---

## Tech Stack

| Concern        | Technology                                             |
| -------------- | ------------------------------------------------------ |
| Framework      | ASP.NET MVC on **.NET 10**                             |
| Language       | C# (nullable reference types, implicit usings)         |
| Data access    | Entity Framework Core 10                               |
| Database       | SQLite (`bookreader.db`)                               |
| Identity       | ASP.NET Core Identity                                  |
| Real-time      | SignalR                                                |
| Frontend       | Razor Views + Bootstrap (responsive, mobile-first)     |
| External API   | Google Books                                           |
| Testing        | xUnit, Moq, EF Core InMemory                           |

---

## Solution Structure

```
Book-Reader-AI/
├── BookReaderApp/                # Main ASP.NET MVC web application
│   ├── Controllers/              # Thin controllers, delegate to services
│   ├── Services/                 # Business logic layer (interface + implementation per service)
│   ├── Repositories/             # EF Core data access, implement IRepository<T>
│   ├── Models/                   # Domain models + ViewModels
│   ├── Data/                     # ApplicationDbContext, DbInitializer (seeding)
│   ├── Hubs/                     # SignalR ChatHub
│   ├── Adapters/                 # Host-side implementations of the messaging module's abstractions
│   ├── Configuration/            # Strongly-typed options (e.g. GoogleBooksOptions)
│   ├── Migrations/               # EF Core migrations for the Identity/main context
│   ├── Views/                    # Razor templates
│   └── wwwroot/                  # Static assets (CSS, JS, images)
│
├── BookReaderApp.Messaging/      # Self-contained messaging module (class library)
│   ├── Abstractions/             # IFriendshipChecker, IMessageNotifier (implemented by the host)
│   ├── Models/                   # Conversation, DirectMessage
│   ├── Data/                     # MessagingDbContext (own schema + migrations history table)
│   ├── Repositories/ Services/   # Messaging data access & logic
│   └── Migrations/               # EF Core migrations for the messaging context
│
└── BookReaderApp.Tests/          # xUnit test project (Services + Data)
```

The messaging module shares the physical SQLite database but keeps its own schema and a separate
migrations history table (`__EFMigrationsHistory_Messaging`), so the two `DbContext`s never collide.
It depends only on EF Core — the storage provider and the friendship/notification adapters are supplied
by the host in `Program.cs`, keeping the library web- and Identity-agnostic.

---

## Architecture

A layered architecture with strict dependency direction:

```
Controllers  →  Services  →  Repositories  →  DbContext
   (thin)      (logic)      (IRepository<T>)   (EF Core)
```

- **Controllers** are thin and delegate to services.
- **Services** hold business logic and consume repositories — they never touch `DbContext` directly.
- **Repositories** implement the repository pattern (`IRepository<T>`, with an open-generic `EfRepository<T>` registration plus specialized repositories).
- **Models vs. ViewModels** are kept separate.
- All I/O is **async** (`Task<T>`, `async`/`await`) — never `.Result` or `.Wait()`.
- List endpoints paginate via a shared `PagedResult<T>` wrapper (default 20 items per page).

See [`AGENTS.md`](AGENTS.md) for the full architecture vision and code standards,
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the as-built architecture, and
[`docs/DEVELOPER_GUIDE.md`](docs/DEVELOPER_GUIDE.md) for a contributor walkthrough (layout,
how to add a feature, and a full route reference).

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- (Optional) A [Google Books API key](https://developers.google.com/books/docs/v1/using#APIKey) for book search/import

### Setup & Run

```powershell
# Restore dependencies
dotnet restore

# Apply EF Core migrations (creates bookreader.db)
dotnet ef database update --project BookReaderApp

# Run the application
dotnet run --project BookReaderApp
```

The app listens on:

- https://localhost:7009
- http://localhost:5235

> The SQLite database is created and seeded on startup, and both the main and messaging migrations are
> applied automatically when the app boots — so the explicit `database update` step above is optional.

### Configuration

Non-secret settings live in `BookReaderApp/appsettings.json`. **Secrets should not be committed.**
Place the Google Books API key in an untracked `appsettings.Local.json` overlay next to it:

```json
{
  "GoogleBooks": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

The key is attached to requests server-side via a typed `HttpClient` and never reaches the browser.

---

## Testing

```powershell
# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~BookReaderApp.Tests.Services.BookServiceTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName=BookReaderApp.Tests.Services.BookServiceTests.GetBookById_WithValidId_ReturnsBook"
```

Tests follow the `Method_Scenario_ExpectedBehavior` naming convention and cover the service and data
layers, mocking external dependencies with Moq and using EF Core's InMemory provider where a context is needed.

---
## UI screenshots
<img width="1895" height="951" alt="Screenshot 2026-06-18 165637" src="https://github.com/user-attachments/assets/72b1e19d-d992-40c9-a3b1-a570b7c7ee99" />
<img width="1363" height="945" alt="Screenshot 2026-06-18 165725" src="https://github.com/user-attachments/assets/c7b92fe9-56a4-4560-aa9d-2cbf5afb469d" />
<img width="1891" height="946" alt="Screenshot 2026-06-18 165740" src="https://github.com/user-attachments/assets/c2860d4d-ee58-4ed5-a437-f2bb4082ed48" />
<img width="482" height="579" alt="Screenshot 2026-06-19 105432" src="https://github.com/user-attachments/assets/0ccf7e56-596f-49cb-9dc6-3cbd6c355ce6" />
<img width="798" height="423" alt="Screenshot 2026-06-18 165752" src="https://github.com/user-attachments/assets/33addec3-f7d5-4323-8c94-7a8bbb988b6c" />
<img width="1865" height="948" alt="Screenshot 2026-06-18 165850" src="https://github.com/user-attachments/assets/f9039c00-b6fd-4544-8c2a-1a7f16b0b757" />
<img width="718" height="746" alt="Screenshot 2026-06-18 165926" src="https://github.com/user-attachments/assets/2d74866b-34ce-4ed2-9ace-474489106f2c" />
<img width="685" height="781" alt="Screenshot 2026-06-18 170003" src="https://github.com/user-attachments/assets/9c447f77-41e8-4882-ad09-68dd6e32c139" />
<img width="678" height="605" alt="Screenshot 2026-06-18 170015" src="https://github.com/user-attachments/assets/fe26a9fd-ed5f-42ea-9906-7e5a8c5d25da" />
<img width="1894" height="944" alt="Screenshot 2026-06-18 192305" src="https://github.com/user-attachments/assets/c574275b-72e4-480a-98df-897ff30a077d" />
<img width="1871" height="944" alt="Screenshot 2026-06-18 205727" src="https://github.com/user-attachments/assets/311d6f53-cd13-4cf4-a770-f1bdac38f5b1" />
<img width="600" height="592" alt="Screenshot 2026-06-19 103849" src="https://github.com/user-attachments/assets/8ab256c5-cf0f-4d28-9071-f98e88a46c67" />

## License

Licensed under the [MIT License](LICENSE). © 2026 Hristomira Sultova-Stoycheva.
