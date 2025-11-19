using LibrarySystem.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.DBContext
{
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> opts) : base(opts) { }

        public DbSet<Book> Books { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<LendingRecord> Lendings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes
            modelBuilder.Entity<Book>().HasIndex(b => b.BookId).IsUnique();
            modelBuilder.Entity<Book>().HasIndex(b => b.Title);
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<LendingRecord>().HasIndex(l => new { l.BookId, l.UserId });
            modelBuilder.Entity<LendingRecord>().HasIndex(l => l.BorrowedAt);
            modelBuilder.Entity<LendingRecord>().HasIndex(l => l.ReturnedAt);

            // Seed data (static deterministic values required for EF Core migrations)
            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    Id = 1,
                    BookId = 100001,
                    Title = "Clean Code",
                    Author = "Robert C. Martin",
                    Pages = 464,
                    TotalCopies = 5,
                    AvailableCopies = 4 // One copy lent out below
                },
                new Book
                {
                    Id = 2,
                    BookId = 100002,
                    Title = "Refactoring",
                    Author = "Martin Fowler",
                    Pages = 448,
                    TotalCopies = 3,
                    AvailableCopies = 2 // One active lending
                },
                new Book
                {
                    Id = 3,
                    BookId = 100003,
                    Title = "Design Patterns",
                    Author = "Erich Gamma et al.",
                    Pages = 395,
                    TotalCopies = 4,
                    AvailableCopies = 4
                }
            );

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Name = "Alice Johnson", Email = "alice@example.com" },
                new User { Id = 2, Name = "Bob Smith", Email = "bob@example.com" },
                new User { Id = 3, Name = "Charlie Young", Email = "charlie@example.com" }
            );

            // Lending seed (BookId refers to Book primary key Id, UserId to User Id)
            modelBuilder.Entity<LendingRecord>().HasData(
                new LendingRecord
                {
                    Id = 1,
                    BookId = 1,
                    UserId = 1,
                    BorrowedAt = new DateTime(2025, 01, 10, 0, 0, 0, DateTimeKind.Utc),
                    ReturnedAt = null, // Active lending
                    PagesAtBorrow = 464
                },
                new LendingRecord
                {
                    Id = 2,
                    BookId = 2,
                    UserId = 2,
                    BorrowedAt = new DateTime(2025, 01, 12, 0, 0, 0, DateTimeKind.Utc),
                    ReturnedAt = null,
                    PagesAtBorrow = 448
                },
                new LendingRecord
                {
                    Id = 3,
                    BookId = 1,
                    UserId = 3,
                    BorrowedAt = new DateTime(2025, 01, 05, 0, 0, 0, DateTimeKind.Utc),
                    ReturnedAt = new DateTime(2025, 01, 14, 0, 0, 0, DateTimeKind.Utc),
                    PagesAtBorrow = 464
                }
            );
        }
    }
}
