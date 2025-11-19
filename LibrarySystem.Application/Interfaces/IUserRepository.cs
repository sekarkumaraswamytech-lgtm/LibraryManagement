using LibrarySystem.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LibrarySystem.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId, CancellationToken ct);
        Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct);
    }
}
