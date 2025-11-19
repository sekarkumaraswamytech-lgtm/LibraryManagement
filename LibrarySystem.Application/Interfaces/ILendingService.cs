using LibrarySystem.Domain.Models;

namespace LibrarySystem.Application.Interfaces
{
    public interface ILendingService
    {
        Task<IEnumerable<Book>> GetRelatedBooksAsync(int bookId, CancellationToken ct);
        Task RecordBorrowAsync(int userId, int bookId, CancellationToken ct);
        Task RecordReturnAsync(int lendingId, CancellationToken ct);
    }
}
