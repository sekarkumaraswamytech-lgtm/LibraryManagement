using LibrarySystem.Application.Interfaces;
using LibrarySystem.Domain.Models;
using LibrarySystem.Infrastructure.DBContext;
using Microsoft.EntityFrameworkCore;
using LibrarySystem.Application.Middleware;

namespace LibrarySystem.Infrastructure.Repositories
{
    public class LendingRepository : ILendingRepository
    {
        private readonly LibraryDbContext _ctx;
        public LendingRepository(LibraryDbContext ctx) => _ctx = ctx;

        public Task<LendingRecord?> GetLendingRecordAsync(int userId, int bookId, CancellationToken ct) =>
            _ctx.Lendings.AsNoTracking()
                .FirstOrDefaultAsync(l => l.UserId == userId && l.BookId == bookId, ct);

        public async Task<IEnumerable<LendingRecord>> GetLendingsInRangeAsync(DateTime from, DateTime to, CancellationToken ct) =>
            await _ctx.Lendings
                .Where(l => l.BorrowedAt >= from && l.BorrowedAt <= to)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<IEnumerable<Book>> GetRelatedBooksAsync(int bookId, CancellationToken ct)
        {
            var userIds = await _ctx.Lendings
                .Where(l => l.BookId == bookId)
                .Select(l => l.UserId)
                .Distinct()
                .ToListAsync(ct);

            if (!userIds.Any()) return Enumerable.Empty<Book>();

            var relatedIds = await _ctx.Lendings
                .Where(l => userIds.Contains(l.UserId) && l.BookId != bookId)
                .Select(l => l.BookId)
                .Distinct()
                .ToListAsync(ct);

            if (!relatedIds.Any()) return Enumerable.Empty<Book>();

            return await _ctx.Books
                .Where(b => relatedIds.Contains(b.Id))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task AddLendingAsync(int userId, int bookId, DateTime borrowedAt, CancellationToken ct)
        {
            _ctx.Lendings.Add(new LendingRecord
            {
                UserId = userId,
                BookId = bookId,
                BorrowedAt = borrowedAt
            });
            try
            {
                await _ctx.SaveChangesAsync(ct);
            }
            catch (DbUpdateException dbEx)
            {
                throw new DataAccessException("Failed to save lending record.", "lending_add_dbupdate_error", dbEx);
            }
        }

        public async Task MarkAsReturnedAsync(int lendingId, DateTime returnedAt, CancellationToken ct)
        {
            var record = await _ctx.Lendings.FindAsync(new object?[] { lendingId }, ct);
            if (record == null)
                throw new NotFoundException($"Lending record {lendingId} not found.");

            record.ReturnedAt = returnedAt;
            try
            {
                await _ctx.SaveChangesAsync(ct);
            }
            catch (DbUpdateException dbEx)
            {
                throw new DataAccessException("Failed to update lending record.", "lending_return_dbupdate_error", dbEx);
            }
        }
    }
}