using LibrarySystem.Application.Interfaces;
using LibrarySystem.Infrastructure.Repositories;
using LibrarySystem.Infrastructure.Tests.Util;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Xunit;

namespace LibrarySystem.Infrastructure.Tests.Repositories
{
    public class UserRepositoryTests
    {
        private IUserRepository CreateRepo()
        {
            var ctx = TestDbContextFactory.Create();
            return new UserRepository(ctx, NullLogger<UserRepository>.Instance);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsUser()
        {
            var repo = CreateRepo();

            var user = await repo.GetByIdAsync(1);

            user.Should().NotBeNull();
            user!.Email.Should().Be("alice@test.local");
        }

        [Fact]
        public async Task GetByIdsAsync_ReturnsSubset()
        {
            var repo = CreateRepo();

            var users = await repo.GetByIdsAsync(new[] { 1, 3 });

            users.Should().HaveCount(2);
            users.Select(u => u.Id).Should().BeEquivalentTo(new[] { 1, 3 });
        }
    }
}