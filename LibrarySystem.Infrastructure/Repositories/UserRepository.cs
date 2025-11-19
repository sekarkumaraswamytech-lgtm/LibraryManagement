using LibrarySystem.Application.Interfaces;
using LibrarySystem.Domain.Models;
using LibrarySystem.Infrastructure.DBContext;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly LibraryDbContext _ctx;
        public UserRepository(LibraryDbContext ctx) => _ctx = ctx;

        public Task<User?> GetByIdAsync(int userId, CancellationToken ct) =>
            _ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

        public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var idList = ids.Distinct().ToList();
            return await _ctx.Users
                .Where(u => idList.Contains(u.Id))
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
