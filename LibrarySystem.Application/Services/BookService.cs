using LibrarySystem.Domain.Models;
using LibrarySystem.Application.Interfaces;
using Microsoft.Extensions.Logging;
using LibrarySystem.Application.Middleware;

namespace LibrarySystem.Application.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _books;
        private readonly ILendingRepository _lendings;
        private readonly IUserRepository _users;
        private readonly IStructuredLogger _log;
        private const double DefaultPagesPerHour = 30;

        public BookService(
            IBookRepository books,
            ILendingRepository lendings,
            IUserRepository users,
            ILogger<BookService>? logger = null)
        {
            _books = books;
            _lendings = lendings;
            _users = users;
            _log = new StructuredLogger<BookService>(logger);
        }

        public async Task<IEnumerable<Book>> GetMostBorrowedBooksAsync(CancellationToken ct)
        {
            _log.Info("Retrieving most borrowed book(s)");
            var list = await _books.GetMostBorrowedAsync(top: 1, ct);
            if (!list.Any())
            {
                _log.Warn("No borrowing records found");
            }
            return list;
        }

        public async Task<IEnumerable<Book>> GetAllAsync(CancellationToken ct)
        {
            _log.Info("Retrieving all books");
            return await _books.GetAllAsync(ct);
        }

        public async Task<Book?> GetByIdAsync(int bookId, CancellationToken ct)
        {
            if (bookId <= 0)
                throw new ValidationException($"BookId must be positive. Provided: {bookId}");

            _log.Info("Attempting to retrieve book", new { bookId });

            // Avoid enumerating full list if not needed
            var book = await _books.GetByIdAsync(bookId, ct);

            if (book is null)
            {
                _log.Warn("Book not found", new { bookId });
                return null;
            }

            _log.Info("Book retrieved", new { book.Id, book.Title });
            return book;
        }

        public async Task<double> EstimateReadingPaceAsync(int userId, int bookId, CancellationToken ct)
        {
            if (userId <= 0) throw new ValidationException($"UserId must be positive. Provided: {userId}");
            if (bookId <= 0) throw new ValidationException($"BookId must be positive. Provided: {bookId}");

            _log.Info("Estimating reading time", new { userId, bookId });

            var user = await _users.GetByIdAsync(userId, ct);
            if (user is null)
                throw new NotFoundException($"User {userId} not found.");

            var book = await GetByIdAsync(bookId, ct);
            if (book is null)
                throw new NotFoundException($"Book {bookId} not found.");

            var from = DateTime.UtcNow.AddDays(-180);
            var to = DateTime.UtcNow;
            var range = await _lendings.GetLendingsInRangeAsync(from, to, ct);

            var userReturned = range
                .Where(r => r.UserId == userId && r.ReturnedAt != null)
                .ToList();

            double pagesPerHour;
            if (!userReturned.Any())
            {
                pagesPerHour = DefaultPagesPerHour;
                _log.Warn("No historical lending records for user; using default pages/hour", new { userId, DefaultPagesPerHour });
            }
            else
            {
                var speeds = userReturned
                    .Select(r =>
                    {
                        var hours = (r.ReturnedAt!.Value - r.BorrowedAt).TotalHours;
                        if (hours <= 0) return DefaultPagesPerHour;
                        var pages = r.PagesAtBorrow > 0 ? r.PagesAtBorrow : book.Pages;
                        return pages / hours;
                    })
                    .Where(v => v > 0)
                    .ToList();

                pagesPerHour = speeds.Any() ? speeds.Average() : DefaultPagesPerHour;
            }

            if (pagesPerHour <= 0)
                pagesPerHour = DefaultPagesPerHour;

            var estimatedHours = Math.Round(book.Pages / pagesPerHour, 2, MidpointRounding.AwayFromZero);

            _log.Info("Estimated reading time calculated",
                new { userId, bookId, book.Pages, pagesPerHour, estimatedHours });

            return estimatedHours;
        }
    }
}