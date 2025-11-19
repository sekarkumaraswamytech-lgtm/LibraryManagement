using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Middleware;
using LibrarySystem.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Application.Services
{
    public class LendingService : ILendingService
    {
        private readonly ILendingRepository _lendings;
        private readonly IBookRepository _books;
        private readonly IUserRepository _users;
        private readonly IStructuredLogger _log;

        public LendingService(
            ILendingRepository lendings,
            IBookRepository books,
            IUserRepository users,
            ILogger<LendingService>? logger = null)
        {
            _lendings = lendings;
            _books = books;
            _users = users;
            _log = new StructuredLogger<LendingService>(logger);
        }

        public async Task<IEnumerable<Book>> GetRelatedBooksAsync(int bookId, CancellationToken ct)
        {
            if (bookId <= 0)
                throw new ValidationException($"BookId must be positive. Provided: {bookId}");

            var book = await _books.GetByIdAsync(bookId, ct);
            if (book is null)
                throw new NotFoundException($"Book {bookId} not found.");

            _log.Info("Fetching related books", new { bookId });
            var related = await _lendings.GetRelatedBooksAsync(bookId, ct);

            if (!related.Any())
                _log.Warn("No related books found", new { bookId });
            else
                _log.Info("Related books loaded", new { bookId, count = related.Count() });

            return related;
        }

        public async Task RecordBorrowAsync(int userId, int bookId, CancellationToken ct)
        {
            if (userId <= 0) throw new ValidationException($"UserId must be positive. Provided: {userId}");
            if (bookId <= 0) throw new ValidationException($"BookId must be positive. Provided: {bookId}");

            _log.Info("Attempting to record borrowing", new { userId, bookId });

            var user = await _users.GetByIdAsync(userId, ct);
            if (user is null)
                throw new NotFoundException($"User {userId} not found.");

            var book = await _books.GetByIdAsync(bookId, ct);
            if (book is null)
                throw new NotFoundException($"Book {bookId} not found.");

            // Ensure user does not already have active lending
            var existing = await _lendings.GetLendingRecordAsync(userId, bookId, ct);
            if (existing is not null && existing.ReturnedAt is null)
                throw new ValidationException($"User {userId} already has an active lending for book {bookId}.");

            // Optimistic concurrency decrement
            var adjusted = await _books.TryAdjustAvailableCopiesAsync(bookId, -1, ct);
            if (!adjusted)
                throw new ValidationException($"No available copies for book {book.Title} ({bookId}).");

            await _lendings.AddLendingAsync(userId, bookId, DateTime.UtcNow, ct);
            _log.Info("Borrowing recorded", new { userId, bookId });
        }

        public async Task RecordReturnAsync(int lendingId, CancellationToken ct)
        {
            if (lendingId <= 0)
                throw new ValidationException($"LendingId must be positive. Provided: {lendingId}");

            _log.Info("Attempting to record return", new { lendingId });

            // Naive fetch – optimize by adding repository GetById if needed
            var range = await _lendings.GetLendingsInRangeAsync(DateTime.UtcNow.AddYears(-5), DateTime.UtcNow.AddMinutes(5), ct);
            var lending = range.FirstOrDefault(l => l.Id == lendingId);

            if (lending is null)
                throw new NotFoundException($"Lending record {lendingId} not found.");

            if (lending.ReturnedAt is not null)
            {
                _log.Warn("Lending already returned", new { lendingId, lending.ReturnedAt });
                return;
            }

            // Increment available copies optimistically
            var success = await _books.TryAdjustAvailableCopiesAsync(lending.BookId, +1, ct);
            if (!success)
            {
                _log.Warn("Failed to increment available copies (concurrency or missing book)", new { lending.BookId });
            }

            await _lendings.MarkAsReturnedAsync(lendingId, DateTime.UtcNow, ct);
            _log.Info("Return recorded", new { lendingId, lending.BookId });
        }
    }
}
