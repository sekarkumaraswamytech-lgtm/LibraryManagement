using LibrarySystem.Infrastructure.DBContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace LibrarySystem.Infrastructure.Tests.Util
{
    internal static class TestDbContextFactory
    {
        public static LibraryDbContext Create(bool seed = true)
        {
            // Use SQLite in-memory for better LINQ translation fidelity than InMemory provider.
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();

            var opts = new DbContextOptionsBuilder<LibraryDbContext>()
                .UseSqlite(conn)
                .EnableSensitiveDataLogging()
                .Options;

            var ctx = new LibraryDbContext(opts);
            ctx.Database.EnsureCreated();

            if (seed)
            {
                Seed(ctx);
            }

            return ctx;
        }

        private static void Seed(LibraryDbContext ctx)
        {
            // Books
            ctx.Books.AddRange(
                new Domain.Models.Book { Id = 1, BookId = 1001, Title = "Clean Code", Author = "Robert C. Martin", Pages = 464, TotalCopies = 5, AvailableCopies = 4 },
                new Domain.Models.Book { Id = 2, BookId = 1002, Title = "Refactoring", Author = "Martin Fowler", Pages = 448, TotalCopies = 3, AvailableCopies = 2 },
                new Domain.Models.Book { Id = 3, BookId = 1003, Title = "Patterns", Author = "GoF", Pages = 395, TotalCopies = 4, AvailableCopies = 4 }
            );

            // Users
            ctx.Users.AddRange(
                new Domain.Models.User { Id = 1, Name = "Alice", Email = "alice@test.local" },
                new Domain.Models.User { Id = 2, Name = "Bob", Email = "bob@test.local" },
                new Domain.Models.User { Id = 3, Name = "Charlie", Email = "charlie@test.local" }
            );

            // Lendings (simulate activity)
            ctx.Lendings.AddRange(
                new Domain.Models.LendingRecord { Id = 1, UserId = 1, BookId = 1, BorrowedAt = DateTime.UtcNow.AddDays(-10), ReturnedAt = DateTime.UtcNow.AddDays(-5), PagesAtBorrow = 464 },
                new Domain.Models.LendingRecord { Id = 2, UserId = 2, BookId = 1, BorrowedAt = DateTime.UtcNow.AddDays(-7), ReturnedAt = null, PagesAtBorrow = 464 },
                new Domain.Models.LendingRecord { Id = 3, UserId = 1, BookId = 2, BorrowedAt = DateTime.UtcNow.AddDays(-3), ReturnedAt = null, PagesAtBorrow = 448 },
                new Domain.Models.LendingRecord { Id = 4, UserId = 3, BookId = 2, BorrowedAt = DateTime.UtcNow.AddDays(-2), ReturnedAt = null, PagesAtBorrow = 448 }
            );

            ctx.SaveChanges();
        }
    }
}