using LibrarySystem.Application.Interfaces;
using LibrarySystem.Infrastructure.Repositories;
using LibrarySystem.Infrastructure.Tests.Util;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Xunit;

namespace LibrarySystem.Infrastructure.Tests.Repositories
{
    public class BookRepositoryTests
    {
        private IBookRepository CreateRepo()
        {
            var ctx = TestDbContextFactory.Create();
            return new BookRepository(ctx, NullLogger<BookRepository>.Instance);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllSeededBooks()
        {
            var repo = CreateRepo();

            var books = await repo.GetAllAsync();

            books.Should().HaveCount(3);
            books.Select(b => b.Title).Should().Contain(new[] { "Clean Code", "Refactoring", "Patterns" });
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectBook()
        {
            var repo = CreateRepo();

            var book = await repo.GetByIdAsync(2);

            book.Should().NotBeNull();
            book!.Title.Should().Be("Refactoring");
        }

        [Fact]
        public async Task GetMostBorrowedAsync_ReturnsTopBookByBorrowCount()
        {
            var repo = CreateRepo();

            var top = await repo.GetMostBorrowedAsync(1);

            top.Should().HaveCount(1);
            top.First().Id.Should().Be(1); // Book 1 has lendings Id=1 & 2 (2 records), Book 2 has lendings Id=3 & 4 (2 records) -> tie; order by Id after join may vary.
        }

        [Fact]
        public async Task GetMostBorrowedAsync_TwoTop_ReturnsTwoBooks()
        {
            var repo = CreateRepo();

            var topTwo = await repo.GetMostBorrowedAsync(2);

            topTwo.Should().HaveCount(2);
        }
    }
}