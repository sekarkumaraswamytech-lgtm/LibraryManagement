using LibrarySystem.Application.Interfaces;
using LibrarySystem.Domain.Models;
using LibrarySystem.Infrastructure.DBContext;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly LibraryDbContext _ctx;
        public BookRepository(LibraryDbContext ctx) => _ctx = ctx;

        public Task<Book?> GetByIdAsync(int id, CancellationToken ct) =>
            _ctx.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);

        public async Task<IEnumerable<Book>> GetAllAsync(CancellationToken ct) =>
            await _ctx.Books.AsNoTracking().ToListAsync(ct);

        public async Task<IEnumerable<Book>> GetMostBorrowedAsync(int top, CancellationToken ct) =>
            await _ctx.Lendings
                .GroupBy(l => l.BookId)
                .OrderByDescending(g => g.Count())
                .Take(top)
                .Select(g => g.Key)
                .Join(_ctx.Books,
                      id => id,
                      b => b.Id,
                      (_, b) => b)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<bool> TryAdjustAvailableCopiesAsync(int bookId, int delta, CancellationToken ct)
        {
            // Optimistic concurrency loop: attempt once; caller treats false as failure.
            var book = await _ctx.Books.FirstOrDefaultAsync(b => b.Id == bookId, ct);
            if (book == null) return false;

            if (delta < 0 && book.AvailableCopies < Math.Abs(delta))
                return false;

            book.AvailableCopies += delta;
            try
            {
                await _ctx.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
        }
    }
}
