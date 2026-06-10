# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Book Reader AI** is an ASP.NET MVC web application serving as a digital bookshelf, reading journal, and book club platform. The project is in early development — the application has not yet been scaffolded.

See `AGENTS.md` for full architecture vision, module breakdown, code standards, and operating manual.

## Build, Run, and Test

Once the ASP.NET MVC project is created, the standard commands will be:

```powershell
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run the application
dotnet run --project BookReaderApp

# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~BookReaderApp.Tests.Services.BookServiceTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName=BookReaderApp.Tests.Services.BookServiceTests.GetBookById_WithValidId_ReturnsBook"

# Apply EF Core migrations
dotnet ef database update --project BookReaderApp
```

## Architecture

The application follows ASP.NET MVC with a layered architecture:

- **Controllers** → thin, delegate to Services
- **Services** → business logic, consume Repositories
- **Repositories** → Entity Framework Core data access, implement `IRepository<T>`
- **Models** → domain models; ViewModels are separate from domain models
- **Views** → Razor templates with Bootstrap responsive layout

All I/O must be async (`Task<T>` return types, `async`/`await`). Never call `.Result` or `.Wait()`.

## Key Conventions

- Repository pattern: services never access `DbContext` directly
- Pagination: all list endpoints default to 20 items per page using a shared `PagedResult<T>` wrapper
- Async naming: suffix all async methods with `Async` (e.g., `GetBooksAsync`)
- Test naming: `Method_Scenario_ExpectedBehavior` (e.g., `AddReview_WithValidInput_ReturnsCreatedReview`)
