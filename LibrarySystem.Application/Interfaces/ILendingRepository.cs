using LibrarySystem.Domain.Models;
using System.Threading;

namespace LibrarySystem.Application.Interfaces
{
    public interface ILendingRepository
    {
        Task<LendingRecord?> GetLendingRecordAsync(int userId, int bookId, CancellationToken ct);
        Task<IEnumerable<LendingRecord>> GetLendingsInRangeAsync(DateTime from, DateTime to, CancellationToken ct);
        Task<IEnumerable<Book>> GetRelatedBooksAsync(int bookId, CancellationToken ct);
        Task AddLendingAsync(int userId, int bookId, DateTime borrowedAt, CancellationToken ct);
        Task MarkAsReturnedAsync(int lendingId, DateTime returnedAt, CancellationToken ct);
    }
}
