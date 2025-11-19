using LibrarySystem.Application.Interfaces;
using LibrarySystem.Infrastructure.Repositories;
using LibrarySystem.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Xunit;
using System.Threading;

namespace LibrarySystem.Tests.Tests.RepositoryTests
{
    public class UsersRepositoryTests
    {
        private IUserRepository CreateRepo()
        {
            var ctx = TestDbContextFactory.Create();
            return new UserRepository(ctx);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsUser()
        {
            var repo = CreateRepo();
            var user = await repo.GetByIdAsync(1, CancellationToken.None);
            user.Should().NotBeNull();
            user!.Email.Should().Be("alice@test.local");
        }

        [Fact]
        public async Task GetByIdsAsync_ReturnsSubset()
        {
            var repo = CreateRepo();
            var users = await repo.GetByIdsAsync(new[] { 1, 3 }, CancellationToken.None);
            users.Should().HaveCount(2);
        }
    }
}
