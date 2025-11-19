using LibrarySystem.gRpcBooks.Services;
using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Services;
using LibrarySystem.Infrastructure.DBContext;
using LibrarySystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// Use a unique InMemory database name per host instance to avoid duplicate seeding collisions in parallel tests.
var dbName = $"BooksGrpcDb_{Guid.NewGuid():N}";
builder.Services.AddDbContext<LibraryDbContext>(o => o.UseInMemoryDatabase(dbName));

// gRPC + exception interceptor
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcExceptionInterceptor>();
    // options.Interceptors.Add<GrpcCorrelationInterceptor>(); // if implemented
});

// Repositories
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<ILendingRepository, LendingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ILendingService, LendingService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Safe seed (idempotent & duplicate-key protected)
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

    // Only add if the specific Ids are absent
    if (!ctx.Books.Any(b => b.Id == 1))
    {
        ctx.Books.Add(new LibrarySystem.Domain.Models.Book
        {
            Id = 1,
            BookId = 1001,
            Title = "Clean Code",
            Author = "Robert C. Martin",
            Pages = 464,
            TotalCopies = 5,
            AvailableCopies = 5
        });
    }

    if (!ctx.Books.Any(b => b.Id == 2))
    {
        ctx.Books.Add(new LibrarySystem.Domain.Models.Book
        {
            Id = 2,
            BookId = 1002,
            Title = "Refactoring",
            Author = "Martin Fowler",
            Pages = 448,
            TotalCopies = 3,
            AvailableCopies = 3
        });
    }

    ctx.SaveChanges();
}

app.MapGrpcService<BookgRpcService>();
app.Run();

// Required for WebApplicationFactory in tests
public partial class Program { }
