using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Middleware;
using LibrarySystem.Application.Services;
using LibrarySystem.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LibrarySystem.Tests.Tests.ServiceTests
{
    public class BookServiceTests
    {
        private (BookService svc,
                 Mock<IBookRepository> books,
                 Mock<ILendingRepository> lendings,
                 Mock<IUserRepository> users) Create()
        {
            var books = new Mock<IBookRepository>();
            var lendings = new Mock<ILendingRepository>();
            var users = new Mock<IUserRepository>();
            var svc = new BookService(books.Object, lendings.Object, users.Object, NullLogger<BookService>.Instance);
            return (svc, books, lendings, users);
        }

        [Fact]
        public async Task GetByIdAsync_ValidationError_WhenIdInvalid()
        {
            var (svc, _, _, _) = Create();
            Func<Task> act = () => svc.GetByIdAsync(0, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            var (svc, books, _, _) = Create();
            books.Setup(b => b.GetByIdAsync(123, CancellationToken.None)).ReturnsAsync((Book?)null);

            var result = await svc.GetByIdAsync(123, CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsBook_WhenFound()
        {
            var (svc, books, _, _) = Create();
            var book = new Book { Id = 5, Title = "Some Title" };
            books.Setup(b => b.GetByIdAsync(5, CancellationToken.None)).ReturnsAsync(book);

            var result = await svc.GetByIdAsync(5, CancellationToken.None);

            result.Should().NotBeNull();
            result!.Title.Should().Be("Some Title");
        }

        [Fact]
        public async Task EstimateReadingPaceAsync_UsesDefault_WhenNoHistory()
        {
            var (svc, books, lendings, users) = Create();
            var book = new Book { Id = 10, Pages = 300 };
            users.Setup(u => u.GetByIdAsync(1, CancellationToken.None)).ReturnsAsync(new User { Id = 1, Name = "User", Email = "user@test.local" });
            books.Setup(b => b.GetByIdAsync(10, CancellationToken.None)).ReturnsAsync(book);
            lendings.Setup(l => l.GetLendingsInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<LendingRecord>());

            var hours = await svc.EstimateReadingPaceAsync(1, 10, CancellationToken.None);

            hours.Should().Be(Math.Round(300 / 30d, 2));
        }

        [Fact]
        public async Task EstimateReadingPaceAsync_ComputesAverage_FromHistory()
        {
            var (svc, books, lendings, users) = Create();
            var book = new Book { Id = 11, Pages = 200 };
            users.Setup(u => u.GetByIdAsync(2, CancellationToken.None)).ReturnsAsync(new User { Id = 2, Name = "User", Email = "user2@test.local" });
            books.Setup(b => b.GetByIdAsync(11, CancellationToken.None)).ReturnsAsync(book);

            var lendingHistory = new[]
            {
                new LendingRecord
                {
                    UserId = 2, BookId = 11,
                    BorrowedAt = DateTime.UtcNow.AddHours(-10),
                    ReturnedAt = DateTime.UtcNow.AddHours(-5),
                    PagesAtBorrow = 200
                },
                new LendingRecord
                {
                    UserId = 2, BookId = 11,
                    BorrowedAt = DateTime.UtcNow.AddHours(-6),
                    ReturnedAt = DateTime.UtcNow.AddHours(-3),
                    PagesAtBorrow = 200
                }
            };

            lendings.Setup(l => l.GetLendingsInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(lendingHistory);

            var hours = await svc.EstimateReadingPaceAsync(2, 11, CancellationToken.None);

            hours.Should().BeGreaterThan(0);
            hours.Should().BeLessThan(10);
        }

        [Fact]
        public async Task GetMostBorrowedBooksAsync_ReturnsList()
        {
            var (svc, books, _, _) = Create();
            books.Setup(b => b.GetMostBorrowedAsync(1, CancellationToken.None))
                .ReturnsAsync(new[] { new Book { Id = 1, Title = "Clean Code", Pages = 464 } });

            var result = await svc.GetMostBorrowedBooksAsync(CancellationToken.None);

            result.Should().HaveCount(1);
        }
    }
}