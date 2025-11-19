using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Services;
using LibrarySystem.Infrastructure.DBContext;
using LibrarySystem.Infrastructure.Repositories;
using LibrarySystem.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using LibrarySystem.Application.Middleware;
using FluentAssertions;
using Xunit;
using System.Threading;

namespace LibrarySystem.Tests.Tests.FunctionalTests
{
    /// <summary>
    /// Functional tests: exercise core business flows at the application service layer
    /// against a real (in-memory) EF Core context and concrete repositories.
    /// These do NOT traverse gRPC or HTTP but validate multi-step operations and
    /// cross-repository interactions.
    /// </summary>
    public class LibraryFunctionalTests
    {
        private (LibraryDbContext ctx,
                 IBookRepository books,
                 IUserRepository users,
                 ILendingRepository lendings,
                 BookService bookService,
                 UserService userService,
                 LendingService lendingService) Bootstrap()
        {
            var ctx = TestDbContextFactory.Create(true);
            var books = new BookRepository(ctx);
            var users = new UserRepository(ctx);
            var lendings = new LendingRepository(ctx);
            var bookSvc = new BookService(books, lendings, users, NullLogger<BookService>.Instance);
            var userSvc = new UserService(users, lendings, NullLogger<UserService>.Instance);
            var lendingSvc = new LendingService(lendings, books, users, NullLogger<LendingService>.Instance);
            return (ctx, books, users, lendings, bookSvc, userSvc, lendingSvc);
        }

        [Fact]
        public async Task BorrowFlow_DecrementsAvailableCopies()
        {
            var (ctx, _, _, _, _, _, lendingSvc) = Bootstrap();
            var userId = 1;
            var bookId = 3; // Book 3 has AvailableCopies = 4 initially
            var before = ctx.Books.Single(b => b.Id == bookId).AvailableCopies;

            await lendingSvc.RecordBorrowAsync(userId, bookId, CancellationToken.None);

            var after = ctx.Books.Single(b => b.Id == bookId).AvailableCopies;
            after.Should().Be(before - 1);
        }

        [Fact]
        public async Task BorrowFlow_ActiveExisting_Throws()
        {
            var (_, _, _, lendings, _, _, lendingSvc) = Bootstrap();
            // Seed already has active lending: user 1 book 2? Ensure we simulate by adding if needed
            var act = () => lendingSvc.RecordBorrowAsync(1, 2, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task ReturnFlow_IncrementsAvailableCopies()
        {
            var (ctx, _, _, lendings, _, _, lendingSvc) = Bootstrap();
            await lendingSvc.RecordBorrowAsync(1, 3, CancellationToken.None);
            var midCopies = ctx.Books.Single(b => b.Id == 3).AvailableCopies;
            var lending = ctx.Lendings.Single(l => l.UserId == 1 && l.BookId == 3);
            await lendingSvc.RecordReturnAsync(lending.Id, CancellationToken.None);
            var finalCopies = ctx.Books.Single(b => b.Id == 3).AvailableCopies;
            finalCopies.Should().Be(midCopies + 1);
        }
    }
}