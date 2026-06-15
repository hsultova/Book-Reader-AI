using BookReaderApp.Adapters;
using BookReaderApp.Configuration;
using BookReaderApp.Data;
using BookReaderApp.Hubs;
using BookReaderApp.Messaging.Abstractions;
using BookReaderApp.Messaging.Data;
using BookReaderApp.Messaging.Repositories;
using BookReaderApp.Messaging.Services;
using BookReaderApp.Models;
using BookReaderApp.Repositories;
using BookReaderApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Untracked overlay holding secrets (e.g. the Google Books API key)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Messaging module: shares the physical SQLite database but keeps its own schema and a
// separate migrations history table so the two contexts never collide.
builder.Services.AddDbContext<MessagingDbContext>(options =>
    options.UseSqlite(connectionString, sqlite =>
        sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Messaging")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password policy (AGENTS.md §Security).
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Lockout: 5 failed attempts -> 15-minute lockout.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Data-access layer. Open-generic registration gives every entity a CRUD repository.
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IRepository<Book>>(sp => sp.GetRequiredService<IBookRepository>());
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<IUserBookRepository, UserBookRepository>();
builder.Services.AddScoped<IShelfRepository, ShelfRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReviewLikeRepository, ReviewLikeRepository>();
builder.Services.AddScoped<IReviewCommentRepository, ReviewCommentRepository>();
builder.Services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
builder.Services.AddScoped<IRepository<FriendRequest>>(sp => sp.GetRequiredService<IFriendRequestRepository>());

// Business-logic layer. Scoped to match the Identity managers / DbContext it depends on.
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserBookService, UserBookService>();
builder.Services.AddScoped<IShelfService, ShelfService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IReviewLikeService, ReviewLikeService>();
builder.Services.AddScoped<IReviewCommentService, ReviewCommentService>();
builder.Services.AddScoped<IFriendRequestService, FriendRequestService>();
builder.Services.AddScoped<IUpdatesService, UpdatesService>();

// Messaging module. Repositories + service live in the BookReaderApp.Messaging class
// library; the two adapters below are the host-side implementations of its abstractions
// (friendship check delegates to the existing friend-request service; notifications go
// out over SignalR).
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IDirectMessageRepository, DirectMessageRepository>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<IFriendshipChecker, FriendRequestFriendshipChecker>();
builder.Services.AddScoped<IMessageNotifier, SignalRMessageNotifier>();

// Google Books integration. Options bind the (backend-only) API key; the typed HttpClient
// is the only path to Google — the key is attached server-side and never reaches the browser.
builder.Services.Configure<GoogleBooksOptions>(
    builder.Configuration.GetSection(GoogleBooksOptions.SectionName));
builder.Services.AddHttpClient<IGoogleBooksService, GoogleBooksService>();

// Antiforgery (CSRF) on every unsafe verb without annotating each action.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Real-time transport for direct messaging.
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<ChatHub>("/hubs/chat");

await DbInitializer.SeedAsync(app.Services);

// Apply the messaging module's migrations (DbInitializer migrates the Identity context).
using (var scope = app.Services.CreateScope())
{
    var messagingDb = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
    await messagingDb.Database.MigrateAsync();
}

app.Run();
