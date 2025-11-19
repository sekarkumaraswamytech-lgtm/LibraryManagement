using LibrarySystem.Application.Interfaces;
using LibrarySystem.Infrastructure.Repositories;
using LibrarySystem.Tests.Utils;

namespace LibrarySystem.Tests.Tests.RepositoryTests
{
    public class BookRepositoryTests
    {
        private IBookRepository CreateRepo()
        {
            var ctx = TestDbContextFactory.Create();
            return new BookRepository(ctx);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllSeededBooks()
        {
            var repo = CreateRepo();
            var books = await repo.GetAllAsync(CancellationToken.None);
            books.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectBook()
        {
            var repo = CreateRepo();
            var book = await repo.GetByIdAsync(2, CancellationToken.None);
            book.Should().NotBeNull();
            book!.Title.Should().Be("Refactoring");
        }

        [Fact]
        public async Task GetMostBorrowedAsync_TopOne()
        {
            var repo = CreateRepo();
            var top = await repo.GetMostBorrowedAsync(1, CancellationToken.None);
            top.Should().HaveCount(1);
        }

        [Fact]
        public async Task TryAdjustAvailableCopies_Decrements()
        {
            var repo = CreateRepo();
            var success = await repo.TryAdjustAvailableCopiesAsync(1, -1, CancellationToken.None);
            success.Should().BeTrue();
            var updated = await repo.GetByIdAsync(1, CancellationToken.None);
            updated!.AvailableCopies.Should().Be(3);
        }
    }
}
