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

<img width="1911" height="940" alt="image" src="https://github.com/user-attachments/assets/7fbf00cb-1985-47f0-9ba9-0e9a557ee720" />
<img width="1879" height="935" alt="image" src="https://github.com/user-attachments/assets/10826740-4177-4bff-a930-0c17bc371ab0" />
<img width="1906" height="923" alt="image" src="https://github.com/user-attachments/assets/909d6f0b-edf7-479b-86d2-c60a5ed8e596" />




## License

Licensed under the [MIT License](LICENSE). © 2026 Hristomira Sultova-Stoycheva.
