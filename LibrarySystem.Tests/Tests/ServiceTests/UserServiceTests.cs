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
    public class UserServiceTests
    {
        private (UserService svc,
                 Mock<IUserRepository> users,
                 Mock<ILendingRepository> lendings) Create()
        {
            var users = new Mock<IUserRepository>();
            var lendings = new Mock<ILendingRepository>();
            var svc = new UserService(users.Object, lendings.Object, NullLogger<UserService>.Instance);
            return (svc, users, lendings);
        }

        [Fact]
        public async Task GetByIdAsync_InvalidId_Throws()
        {
            var (svc, _, _) = Create();
            Func<Task> act = () => svc.GetByIdAsync(0, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*positive*");
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            var (svc, users, _) = Create();
            users.Setup(u => u.GetByIdAsync(5, CancellationToken.None))
                 .ReturnsAsync((User?)null);

            var result = await svc.GetByIdAsync(5, CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Found_ReturnsUser()
        {
            var (svc, users, _) = Create();
            users.Setup(u => u.GetByIdAsync(7, CancellationToken.None))
                 .ReturnsAsync(new User { Id = 7, Name = "Alice", Email = "alice@test.local" });

            var result = await svc.GetByIdAsync(7, CancellationToken.None);

            result.Should().NotBeNull();
            result!.Email.Should().Be("alice@test.local");
        }

        [Fact]
        public async Task GetMostActiveUsersAsync_Raw_InvalidDates_Throws()
        {
            var (svc, _, _) = Create();
            Func<Task> act = () => svc.GetMostActiveUsersAsync("bad", "2025-01-01", CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*Invalid From date*");
        }

        [Fact]
        public async Task GetMostActiveUsersAsync_Raw_Missing_Throws()
        {
            var (svc, _, _) = Create();
            await Assert.ThrowsAsync<ValidationException>(() => svc.GetMostActiveUsersAsync("", "2025-01-01", CancellationToken.None));
        }

        [Fact]
        public async Task GetMostActiveUsersAsync_Range_FromAfterTo_Throws()
        {
            var (svc, _, _) = Create();
            var from = DateTime.UtcNow;
            var to = DateTime.UtcNow.AddHours(-1);

            await Assert.ThrowsAsync<ValidationException>(() => svc.GetMostActiveUsersAsync(from, to, CancellationToken.None));
        }

        [Fact]
        public async Task GetMostActiveUsersAsync_Range_TooLarge_Throws()
        {
            var (svc, _, _) = Create();
            var from = DateTime.UtcNow.AddYears(-3);
            var to = DateTime.UtcNow;

            await Assert.ThrowsAsync<ValidationException>(() => svc.GetMostActiveUsersAsync(from, to, CancellationToken.None));
        }

        [Fact]
        public async Task GetMostActiveUsersAsync_Range_FutureTo_Throws()
        {
            var (svc, _, _) = Create();
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow.AddHours(6); // > +5 minutes

            await Assert.ThrowsAsync<ValidationException>(() => svc.GetMostActiveUsersAsync(from, to, CancellationToken.None));
        }

        [Fact]
        public async Task GetMostActiveUsersAsync_NoLendings_ReturnsEmpty()
        {
            var (svc, _, lendings) = Create();
            var from = DateTime.UtcNow.AddDays(-10);
            var to = DateTime.UtcNow;

            lendings.Setup(l => l.GetLendingsInRangeAsync(from, to, CancellationToken.None))
                .ReturnsAsync(Array.Empty<LendingRecord>());

            var result = await svc.GetMostActiveUsersAsync(from, to, CancellationToken.None);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMostActiveUsersAsync_ReturnsOrderedUsers()
        {
            var (svc, usersRepo, lendingsRepo) = Create();
            var from = DateTime.UtcNow.AddDays(-30);
            var to = DateTime.UtcNow;

            var lendingData = new[]
            {
                new LendingRecord { UserId = 1, BookId = 10, BorrowedAt = DateTime.UtcNow.AddDays(-5) },
                new LendingRecord { UserId = 2, BookId = 11, BorrowedAt = DateTime.UtcNow.AddDays(-4) },
                new LendingRecord { UserId = 1, BookId = 12, BorrowedAt = DateTime.UtcNow.AddDays(-3) },
                new LendingRecord { UserId = 3, BookId = 13, BorrowedAt = DateTime.UtcNow.AddDays(-2) }
            };

            lendingsRepo.Setup(l => l.GetLendingsInRangeAsync(from, to, CancellationToken.None))
                .ReturnsAsync(lendingData);

            usersRepo.Setup(u => u.GetByIdsAsync(It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { 1, 2, 3 })),
                        CancellationToken.None))
                .ReturnsAsync(new[]
                {
                    new User { Id = 1, Name = "Alice" },
                    new User { Id = 2, Name = "Bob" },
                    new User { Id = 3, Name = "Charlie" }
                });

            var result = (await svc.GetMostActiveUsersAsync(from, to, CancellationToken.None)).ToList();

            result.Should().HaveCount(3);
            result.First().Id.Should().Be(1); // user 1 has 2 lendings
        }

        [Fact]
        public async Task GetMostActiveUsersAsync_SkipsMissingUsers()
        {
            var (svc, usersRepo, lendingsRepo) = Create();
            var from = DateTime.UtcNow.AddDays(-7);
            var to = DateTime.UtcNow;

            var lendingData = new[]
            {
                new LendingRecord { UserId = 1, BookId = 10, BorrowedAt = DateTime.UtcNow.AddDays(-5) },
                new LendingRecord { UserId = 99, BookId = 11, BorrowedAt = DateTime.UtcNow.AddDays(-4) } // user 99 not returned
            };

            lendingsRepo.Setup(l => l.GetLendingsInRangeAsync(from, to, CancellationToken.None))
                .ReturnsAsync(lendingData);

            usersRepo.Setup(u => u.GetByIdsAsync(It.Is<IEnumerable<int>>(ids =>
                        ids.Contains(1) && ids.Contains(99)),
                        CancellationToken.None))
                .ReturnsAsync(new[]
                {
                    new User { Id = 1, Name = "Alice" } // user 99 intentionally missing
                });

            var result = (await svc.GetMostActiveUsersAsync(from, to, CancellationToken.None)).ToList();

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
        }
    }
}