using LibrarySystem.Application.Interfaces;
using LibrarySystem.Infrastructure.Repositories;
using LibrarySystem.Infrastructure.Tests.Util;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Xunit;

namespace LibrarySystem.Infrastructure.Tests.Repositories
{
    public class LendingRepositoryTests
    {
        private (ILendingRepository repo, LibrarySystem.Infrastructure.DBContext.LibraryDbContext ctx) Create()
        {
            var ctx = TestDbContextFactory.Create();
            var repo = new LendingRepository(ctx, NullLogger<LendingRepository>.Instance);
            return (repo, ctx);
        }

        [Fact]
        public async Task GetLendingRecordAsync_ReturnsRecord()
        {
            var (repo, _) = Create();

            var record = await repo.GetLendingRecordAsync(1, 1);

            record.Should().NotBeNull();
            record!.UserId.Should().Be(1);
            record.BookId.Should().Be(1);
        }

        [Fact]
        public async Task GetLendingsInRangeAsync_ReturnsFilteredRecords()
        {
            var (repo, _) = Create();

            var from = DateTime.UtcNow.AddDays(-8);
            var to = DateTime.UtcNow;
            var results = await repo.GetLendingsInRangeAsync(from, to);

            results.Should().NotBeEmpty();
            results.All(r => r.BorrowedAt >= from && r.BorrowedAt <= to).Should().BeTrue();
        }

        [Fact]
        public async Task GetRelatedBooksAsync_ReturnsRelatedBooks()
        {
            var (repo, _) = Create();

            var related = await repo.GetRelatedBooksAsync(1);

            // Users 1 & 2 borrowed book 1; user 1 also borrowed book 2 => related should include book 2
            related.Select(b => b.Id).Should().Contain(2);
        }

        [Fact]
        public async Task AddLendingAsync_AddsRecord()
        {
            var (repo, ctx) = Create();

            await repo.AddLendingAsync(3, 3, DateTime.UtcNow);

            var added = ctx.Lendings.SingleOrDefault(l => l.UserId == 3 && l.BookId == 3);
            added.Should().NotBeNull();
        }

        [Fact]
        public async Task MarkAsReturnedAsync_SetsReturnedAt()
        {
            var (repo, ctx) = Create();

            // Add a new lending
            await repo.AddLendingAsync(3, 3, DateTime.UtcNow.AddHours(-2));
            var lending = ctx.Lendings.Single(l => l.UserId == 3 && l.BookId == 3);

            await repo.MarkAsReturnedAsync(lending.Id, DateTime.UtcNow);

            var updated = ctx.Lendings.Single(l => l.Id == lending.Id);
            updated.ReturnedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task MarkAsReturnedAsync_Nonexistent_ThrowsNotFound()
        {
            var (repo, _) = Create();

            var act = async () => await repo.MarkAsReturnedAsync(999, DateTime.UtcNow);

            await act.Should().ThrowAsync<LibrarySystem.Application.Middleware.NotFoundException>();
        }
    }
}