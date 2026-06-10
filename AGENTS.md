# AGENTS.md - Book Reader Application

## Vision & Core Principles

**Book Reader** is a modern web application that functions as a digital bookshelf, reading journal, and book club community platform. It enables users to:
- Track their reading progress and maintain a personal library
- Write and read book reviews and reading notes
- Discover new titles through recommendations and community reviews
- Connect with other readers and authors

### Design Principles
1. **Responsive & Fast**: The application must feel snappy and responsive across all interactions
2. **Clear Navigation**: Intuitive UI that guides users without cognitive overhead
3. **Non-blocking Operations**: No freezing during heavy lifting (database queries, external APIs, file uploads)
4. **Data Integrity**: Reliable persistence with no data loss or corruption
5. **Adaptive Layout**: Seamless experience on desktop, tablet, and mobile devices
6. **Clean Codebase**: Modular, maintainable, well-documented code that scales

---

## Architecture Overview

### Tech Stack
- **Framework**: ASP.NET MVC (C#)
- **Database**: SQL Server (recommended)
- **Frontend**: Razor Views with Bootstrap/responsive CSS
- **APIs**: RESTful endpoints for AJAX operations and mobile clients
- **Async Patterns**: Task-based async/await for non-blocking I/O

### Project Structure
```
BookReaderApp/
├── Controllers/           # MVC Controllers (API & View endpoints)
├── Models/               # Domain models and view models
├── Views/                # Razor templates (HTML)
├── Services/             # Business logic layer
├── Data/                 # Entity Framework DbContext, migrations
├── Repositories/         # Data access abstractions
├── Infrastructure/       # Cross-cutting concerns (logging, caching)
├── Tests/                # Unit and integration tests
└── wwwroot/              # Static assets (CSS, JS, images)
```

---

## Key Technical Decisions

### Async/Await Throughout
- All I/O operations (database, APIs) must use `async`/`await`
- Controllers must return `Task<ActionResult>`
- Never use `.Result` or `.Wait()` — leads to deadlocks and UI freezing
- Services expose async methods: `GetUserBooksAsync()`, `AddReviewAsync()`

### Repository Pattern
- Repositories abstract data access and enable testing
- Implement `IRepository<T>` for all entity types
- Avoid Entity Framework DbContext injection in controllers
- Services consume repositories

### Dependency Injection
- Use ASP.NET Core's built-in DI container
- Register services in `Startup.cs` or `Program.cs`
- Constructor injection only — no service locator pattern

### Responsive Design
- Mobile-first CSS approach
- Bootstrap grid system for layout
- Test responsive breakpoints (mobile: <576px, tablet: 768px–992px, desktop: >1200px)
- Avoid fixed widths; use relative units

### Caching Strategy
- Cache frequently accessed data (popular books, user profiles)
- Use in-memory caching for session-level data
- Invalidate caches on writes
- Cache timeout: 15–30 minutes for read-only data

### Error Handling
- Return meaningful HTTP status codes (400, 404, 500, etc.)
- Log all errors with context (user ID, operation, timestamp)
- Never expose stack traces to users
- Graceful degradation on external API failures

---

## Code Quality Standards

### Naming Conventions
- **Classes/Methods**: PascalCase (`GetUserBooks`, `BookService`)
- **Properties**: PascalCase (`BookTitle`, `ReadingStatus`)
- **Private fields**: camelCase with underscore prefix (`_bookRepository`)
- **Constants**: UPPER_SNAKE_CASE (`MAX_BOOKS_PER_REQUEST`)

### Async Method Naming
- Suffix with `Async`: `GetBooksAsync()`, `CreateReviewAsync()`
- Use `Task` or `Task<T>` as return type

### Comments
- Add comments only for **why**, not **what**
- Avoid redundant comments (`int count; // the count`)
- Document complex algorithms or non-obvious decisions
- Keep docstring comments concise

### Testing
- Unit tests cover business logic (Services)
- Integration tests verify data layer + service interactions
- Mock external dependencies (APIs, file systems)
- Naming: `[TestClass]` and `[TestMethod]` or xUnit equivalents
- Test naming convention: `Method_Scenario_ExpectedBehavior()`
  - Example: `AddReview_WithValidInput_ReturnsCreatedReview()`

---

## Testing Strategy

### Unit Tests (80% coverage target)
- Test business logic in Services
- Mock repository layer
- Use xUnit or NUnit
- File location: `Tests/Services/UserServiceTests.cs`

### Integration Tests
- Test Service + Repository + DbContext interaction
- Use in-memory or test database
- File location: `Tests/Integration/BookServiceIntegrationTests.cs`

### Performance Tests
- Load testing for concurrent user scenarios
- Database query performance benchmarks
- Async operation response time verification

### Running Tests
```powershell
dotnet test
dotnet test --configuration Release
```

---

## Performance & Stability Guidelines

### Database Optimization
- Use indexes on frequently queried columns (`UserId`, `BookId`, `CreatedDate`)
- Avoid SELECT * — specify columns explicitly
- Use `.AsNoTracking()` in read-only queries
- Implement pagination for large result sets (default: 20 items per page)
- Use `Include()` for eager loading, avoid N+1 queries

### API Response Performance
- Response time target: <200ms for typical operations
- Endpoint timeout: 30 seconds max
- Request body limit: 100MB max
- Rate limiting: 100 requests per minute per user (configurable)

### Frontend Performance
- Bundle and minify CSS/JavaScript
- Lazy load images
- Use AJAX for non-critical updates
- Avoid full page refreshes when possible
- Target Lighthouse score: 85+

### Resource Limits
- Max file upload size: 50MB
- Max books per library view: 1000 (paginate)
- Background job timeout: 5 minutes
- Connection pool size: 20–30 connections

---

## Security Guidelines

### Authentication & Authorization
- Use ASP.NET Core Identity for user management
- Enforce HTTPS everywhere (no HTTP)
- Session timeout: 30 minutes of inactivity
- CSRF protection on all POST/PUT/DELETE endpoints
- Role-based access control (User, Moderator, Admin)

### Data Protection
- Hash passwords with PBKDF2 or bcrypt (never store plaintext)
- Encrypt sensitive data at rest (PII, payment info)
- Sanitize user input to prevent XSS
- Parameterized queries to prevent SQL injection
- No sensitive data in logs or error messages

### API Security
- Validate all input (length, type, format)
- Return HTTP 403 Forbidden for unauthorized access
- Rate limit login attempts (5 attempts, 15-minute lockout)
- Use environment variables for secrets (database connection strings, API keys)
- Never commit `.env` or secrets to version control

### Dependency Management
- Keep NuGet packages up to date
- Review security advisories monthly
- Use `dotnet list package --deprecated` to catch issues

---

## Deployment & Infrastructure

### Build & Release
- Automated build on every commit (CI/CD pipeline)
- All tests must pass before deployment
- Database migrations run automatically during release
- Rollback plan: Keep previous version available for 24 hours

### Monitoring & Logging
- Log to centralized system (Application Insights, Serilog)
- Log levels: Error, Warning, Info (Info for important events)
- Monitor response times, error rates, database connections
- Alert on: >5% error rate, response time >1s, disk space <10%

### Configuration Management
- Environment-specific settings (appsettings.json, appsettings.Production.json)
- Feature flags for gradual rollout of new features
- Database connection strings in secure secret store

---

## Operating Manual for AI Agents

### When Contributing to This Project

#### 1. **Understand the Request Context**
- Clarify whether the task is a new feature, bug fix, or refactor
- Identify which module(s) are affected (Library, Reviews, Clubs, etc.)
- Confirm performance or security constraints if applicable

#### 2. **Design Before Coding**
- Sketch the changes (affected classes, database schema, API endpoints)
- Propose the approach and confirm alignment with architecture
- Identify any cross-module dependencies

#### 3. **Code Implementation**
- Follow naming conventions and code organization standards
- Use async/await for all I/O operations
- Add error handling at boundaries (user input, external APIs)
- Write unit tests alongside code
- Keep methods and classes focused and small

#### 4. **Testing & Verification**
- Run full test suite locally: `dotnet test`
- Manually test the feature in the browser (desktop & mobile)
- Verify performance: no N+1 queries, no blocking operations
- Check responsive design across breakpoints

#### 5. **Documentation**
- Update code comments if implementing complex logic
- Document any new database schema changes
- Add inline comments for non-obvious decisions

#### 6. **Review Readiness**
- Self-review the diff before requesting review
- Ensure commit messages are clear and follow convention
- Verify no debug code, console logs, or secrets are left behind

#### 7. **Breaking Changes**
- Database schema: Include migration file, document in PR
- API endpoints: Add deprecation notice, maintain backward compatibility for 2 releases
- Service methods: Update all call sites, add helpful error messages

## Additional Resources

- **ASP.NET MVC Best Practices**: https://docs.microsoft.com/en-us/aspnet/mvc/
- **Entity Framework Core**: https://docs.microsoft.com/en-us/ef/core/
- **OWASP Top 10**: https://owasp.org/www-project-top-ten/
- **Design Patterns in C#**: https://refactoring.guru/design-patterns/csharp
- **Bootstrap Responsive Design**: https://getbootstrap.com/

---
