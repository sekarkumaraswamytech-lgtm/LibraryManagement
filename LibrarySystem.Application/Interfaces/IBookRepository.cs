using LibrarySystem.Domain.Models;
using System.Threading;

namespace LibrarySystem.Application.Interfaces
{
    public interface IBookRepository
    {
        Task<Book?> GetByIdAsync(int id, CancellationToken ct);
        Task<IEnumerable<Book>> GetAllAsync(CancellationToken ct);
        Task<IEnumerable<Book>> GetMostBorrowedAsync(int top, CancellationToken ct);

        /// <summary>
        /// Attempts to adjust AvailableCopies by delta (negative to decrement, positive to increment)
        /// using optimistic concurrency. Returns false if book not found or insufficient copies.
        /// </summary>
        Task<bool> TryAdjustAvailableCopiesAsync(int bookId, int delta, CancellationToken ct);
    }
}
