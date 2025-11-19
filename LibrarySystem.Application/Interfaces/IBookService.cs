using LibrarySystem.Domain.Models;
using System.Threading;

namespace LibrarySystem.Application.Interfaces
{
    public interface IBookService
    {
        Task<IEnumerable<Book>> GetMostBorrowedBooksAsync(CancellationToken ct);
        Task<IEnumerable<Book>> GetAllAsync(CancellationToken ct);
        Task<Book?> GetByIdAsync(int bookId, CancellationToken ct);
        Task<double> EstimateReadingPaceAsync(int userId, int bookId, CancellationToken ct);
    }
}
