using LibrarySystem.Application.Interfaces;
using LibrarySystem.Infrastructure.Repositories;
using LibrarySystem.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Xunit;
using System.Threading;

namespace LibrarySystem.Tests.Tests.RepositoryTests
{
    public class LendingRepositoryTests
    {
        private (ILendingRepository repo, LibrarySystem.Infrastructure.DBContext.LibraryDbContext ctx) Create()
        {
            var ctx = TestDbContextFactory.Create();
            var repo = new LendingRepository(ctx);
            return (repo, ctx);
        }

        [Fact]
        public async Task GetLendingRecordAsync_ReturnsRecord()
        {
            var (repo, _) = Create();
            var record = await repo.GetLendingRecordAsync(1, 1, CancellationToken.None);
            record.Should().NotBeNull();
        }

        [Fact]
        public async Task GetLendingsInRangeAsync_ReturnsFiltered()
        {
            var (repo, _) = Create();
            var from = DateTime.UtcNow.AddDays(-8);
            var to = DateTime.UtcNow;
            var result = await repo.GetLendingsInRangeAsync(from, to, CancellationToken.None);
            result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetRelatedBooksAsync_ReturnsRelated()
        {
            var (repo, _) = Create();
            var related = await repo.GetRelatedBooksAsync(1, CancellationToken.None);
            related.Select(b => b.Id).Should().Contain(2);
        }

        [Fact]
        public async Task AddLendingAsync_Persists()
        {
            var (repo, ctx) = Create();
            await repo.AddLendingAsync(3, 3, DateTime.UtcNow, CancellationToken.None);
            ctx.Lendings.SingleOrDefault(l => l.UserId == 3 && l.BookId == 3).Should().NotBeNull();
        }

        [Fact]
        public async Task MarkAsReturnedAsync_SetsReturnedAt()
        {
            var (repo, ctx) = Create();
            await repo.AddLendingAsync(3, 3, DateTime.UtcNow.AddHours(-2), CancellationToken.None);
            var lending = ctx.Lendings.Single(l => l.UserId == 3 && l.BookId == 3);
            await repo.MarkAsReturnedAsync(lending.Id, DateTime.UtcNow, CancellationToken.None);
            ctx.Lendings.Single(l => l.Id == lending.Id).ReturnedAt.Should().NotBeNull();
        }
    }
}
