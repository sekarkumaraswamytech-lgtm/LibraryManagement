using System;
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
    public class LendingServiceTests
    {
        private (LendingService svc,
                 Mock<ILendingRepository> lendingRepo,
                 Mock<IBookRepository> bookRepo,
                 Mock<IUserRepository> userRepo) Create()
        {
            var lendingRepo = new Mock<ILendingRepository>();
            var bookRepo = new Mock<IBookRepository>();
            var userRepo = new Mock<IUserRepository>();
            var svc = new LendingService(lendingRepo.Object, bookRepo.Object, userRepo.Object, NullLogger<LendingService>.Instance);
            return (svc, lendingRepo, bookRepo, userRepo);
        }

        [Fact]
        public async Task GetRelatedBooksAsync_InvalidId_Throws()
        {
            var (svc, _, _, _) = Create();
            Func<Task> act = () => svc.GetRelatedBooksAsync(0, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task GetRelatedBooksAsync_NotFoundBook_Throws()
        {
            var (svc, _, bookRepo, _) = Create();
            bookRepo.Setup(b => b.GetByIdAsync(10, CancellationToken.None)).ReturnsAsync((Book?)null);

            Func<Task> act = () => svc.GetRelatedBooksAsync(10, CancellationToken.None);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetRelatedBooksAsync_ReturnsData()
        {
            var (svc, lendingRepo, bookRepo, _) = Create();
            bookRepo.Setup(b => b.GetByIdAsync(10, CancellationToken.None)).ReturnsAsync(new Book { Id = 10, Title = "Any" });
            lendingRepo.Setup(l => l.GetRelatedBooksAsync(10, CancellationToken.None))
                .ReturnsAsync(new[] { new Book { Id = 2, Title = "Refactoring" } });

            var result = await svc.GetRelatedBooksAsync(10, CancellationToken.None);

            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task RecordBorrowAsync_InvalidIds_Throws()
        {
            var (svc, _, _, _) = Create();
            await Assert.ThrowsAsync<ValidationException>(() => svc.RecordBorrowAsync(0, 1, CancellationToken.None));
            await Assert.ThrowsAsync<ValidationException>(() => svc.RecordBorrowAsync(1, 0, CancellationToken.None));
        }

        [Fact]
        public async Task RecordBorrowAsync_UserNotFound_Throws()
        {
            var (svc, _, bookRepo, userRepo) = Create();
            userRepo.Setup(u => u.GetByIdAsync(1, CancellationToken.None)).ReturnsAsync((User?)null);
            Func<Task> act = () => svc.RecordBorrowAsync(1, 10, CancellationToken.None);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task RecordBorrowAsync_BookNotFound_Throws()
        {
            var (svc, _, bookRepo, userRepo) = Create();
            userRepo.Setup(u => u.GetByIdAsync(1, CancellationToken.None)).ReturnsAsync(new User { Id = 1 });
            bookRepo.Setup(b => b.GetByIdAsync(10, CancellationToken.None)).ReturnsAsync((Book?)null);
            Func<Task> act = () => svc.RecordBorrowAsync(1, 10, CancellationToken.None);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task RecordBorrowAsync_ExistingActiveLending_Throws()
        {
            var (svc, lendingRepo, bookRepo, userRepo) = Create();
            userRepo.Setup(u => u.GetByIdAsync(1, CancellationToken.None)).ReturnsAsync(new User { Id = 1 });
            bookRepo.Setup(b => b.GetByIdAsync(10, CancellationToken.None)).ReturnsAsync(new Book { Id = 10, Title = "Book", AvailableCopies = 5 });
            lendingRepo.Setup(l => l.GetLendingRecordAsync(1, 10, CancellationToken.None))
                .ReturnsAsync(new LendingRecord { UserId = 1, BookId = 10, BorrowedAt = DateTime.UtcNow });

            Func<Task> act = () => svc.RecordBorrowAsync(1, 10, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*already has an active lending*");
        }

        [Fact]
        public async Task RecordBorrowAsync_NoAvailableCopies_Throws()
        {
            var (svc, lendingRepo, bookRepo, userRepo) = Create();
            userRepo.Setup(u => u.GetByIdAsync(1, CancellationToken.None)).ReturnsAsync(new User { Id = 1 });
            bookRepo.Setup(b => b.GetByIdAsync(10, CancellationToken.None)).ReturnsAsync(new Book { Id = 10, Title = "Book", AvailableCopies = 0 });
            lendingRepo.Setup(l => l.GetLendingRecordAsync(1, 10, CancellationToken.None))
                .ReturnsAsync((LendingRecord?)null);
            bookRepo.Setup(b => b.TryAdjustAvailableCopiesAsync(10, -1, CancellationToken.None)).ReturnsAsync(false);

            Func<Task> act = () => svc.RecordBorrowAsync(1, 10, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*No available copies*");
        }

        [Fact]
        public async Task RecordBorrowAsync_Success()
        {
            var (svc, lendingRepo, bookRepo, userRepo) = Create();
            userRepo.Setup(u => u.GetByIdAsync(1, CancellationToken.None)).ReturnsAsync(new User { Id = 1 });
            bookRepo.Setup(b => b.GetByIdAsync(10, CancellationToken.None)).ReturnsAsync(new Book { Id = 10, Title = "Book", AvailableCopies = 3 });
            lendingRepo.Setup(l => l.GetLendingRecordAsync(1, 10, CancellationToken.None))
                .ReturnsAsync((LendingRecord?)null);
            bookRepo.Setup(b => b.TryAdjustAvailableCopiesAsync(10, -1, CancellationToken.None)).ReturnsAsync(true);
            lendingRepo.Setup(l => l.AddLendingAsync(1, 10, It.IsAny<DateTime>(), CancellationToken.None)).Returns(Task.CompletedTask);

            await svc.RecordBorrowAsync(1, 10, CancellationToken.None);

            lendingRepo.Verify(l => l.AddLendingAsync(1, 10, It.IsAny<DateTime>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task RecordReturnAsync_InvalidId_Throws()
        {
            var (svc, _, _, _) = Create();
            await Assert.ThrowsAsync<ValidationException>(() => svc.RecordReturnAsync(0, CancellationToken.None));
        }

        [Fact]
        public async Task RecordReturnAsync_NotFound_Throws()
        {
            var (svc, lendingRepo, _, _) = Create();
            lendingRepo.Setup(l => l.GetLendingsInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None))
                .ReturnsAsync(Array.Empty<LendingRecord>());

            await Assert.ThrowsAsync<NotFoundException>(() => svc.RecordReturnAsync(999, CancellationToken.None));
        }

        [Fact]
        public async Task RecordReturnAsync_AlreadyReturned_IsIdempotent()
        {
            var (svc, lendingRepo, bookRepo, _) = Create();
            lendingRepo.Setup(l => l.GetLendingsInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None))
                .ReturnsAsync(new[]
                {
                    new LendingRecord { Id = 55, UserId = 1, BookId = 10, BorrowedAt = DateTime.UtcNow.AddHours(-5), ReturnedAt = DateTime.UtcNow.AddHours(-1) }
                });

            // Should not throw and not increment copies
            await svc.RecordReturnAsync(55, CancellationToken.None);

            bookRepo.Verify(b => b.TryAdjustAvailableCopiesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RecordReturnAsync_Success_IncrementsCopies()
        {
            var (svc, lendingRepo, bookRepo, _) = Create();
            lendingRepo.Setup(l => l.GetLendingsInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None))
                .ReturnsAsync(new[]
                {
                    new LendingRecord { Id = 77, UserId = 1, BookId = 10, BorrowedAt = DateTime.UtcNow.AddHours(-5), ReturnedAt = null }
                });
            bookRepo.Setup(b => b.TryAdjustAvailableCopiesAsync(10, +1, CancellationToken.None)).ReturnsAsync(true);
            lendingRepo.Setup(l => l.MarkAsReturnedAsync(77, It.IsAny<DateTime>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            await svc.RecordReturnAsync(77, CancellationToken.None);

            bookRepo.Verify(b => b.TryAdjustAvailableCopiesAsync(10, +1, CancellationToken.None), Times.Once);
            lendingRepo.Verify(l => l.MarkAsReturnedAsync(77, It.IsAny<DateTime>(), CancellationToken.None), Times.Once);
        }
    }
}