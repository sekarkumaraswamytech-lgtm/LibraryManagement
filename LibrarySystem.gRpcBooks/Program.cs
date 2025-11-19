using LibrarySystem.gRpcBooks.Services;
using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Services;
using LibrarySystem.Infrastructure.DBContext;
using LibrarySystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add logging (optional for integration tests)
builder.Logging.AddConsole();

// gRPC + exception interceptor
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcExceptionInterceptor>(); // Ensure this class exists
    // options.Interceptors.Add<GrpcCorrelationInterceptor>(); // Add if implemented
});

// Persistence (InMemory for test/integration)
builder.Services.AddDbContext<LibraryDbContext>(o => o.UseInMemoryDatabase("BooksGrpcDb"));

// Repositories
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<ILendingRepository, LendingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ILendingService, LendingService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Optional seed (avoid duplicate seeding in parallel tests)
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    if (!ctx.Books.Any())
    {
        ctx.Books.AddRange(
            new LibrarySystem.Domain.Models.Book { Id = 1, BookId = 1001, Title = "Clean Code", Author = "Robert C. Martin", Pages = 464, TotalCopies = 5, AvailableCopies = 5 },
            new LibrarySystem.Domain.Models.Book { Id = 2, BookId = 1002, Title = "Refactoring", Author = "Martin Fowler", Pages = 448, TotalCopies = 3, AvailableCopies = 3 }
        );
        ctx.SaveChanges();
    }
}

app.MapGrpcService<BookgRpcService>();
app.Run();

// Required for WebApplicationFactory in tests
public partial class Program { }
