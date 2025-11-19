using LibrarySystem.Domain.Models;
using System.Threading;

namespace LibrarySystem.Application.Interfaces
{
    /// <summary>
    /// User-related business operations.
    /// Provides raw string and parsed DateTime overloads for most-active user queries.
    /// </summary>
    public interface IUserService
    {
        Task<User?> GetByIdAsync(int userId, CancellationToken ct = default);

        // Added default CancellationToken parameter so existing call sites with only (from,to) still compile.
        Task<IEnumerable<User>> GetMostActiveUsersAsync(string fromRaw, string toRaw, CancellationToken ct = default);

        Task<IEnumerable<User>> GetMostActiveUsersAsync(DateTime from, DateTime to, CancellationToken ct = default);
    }
}
