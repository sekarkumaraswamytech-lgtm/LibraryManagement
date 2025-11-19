using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Middleware;
using LibrarySystem.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LibrarySystem.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _users;
        private readonly ILendingRepository _lendings;
        private readonly IStructuredLogger _log;
        private static readonly TimeSpan MaxRange = TimeSpan.FromDays(365 * 2);
        private static readonly TimeSpan MinRange = TimeSpan.FromMinutes(1);
        private static readonly string[] DateFormats =
        {
            "o","yyyy-MM-dd","yyyy-MM-ddTHH:mm:ssZ","yyyy-MM-ddTHH:mm:ss.fffZ","MM/dd/yyyy","dd/MM/yyyy"
        };

        public UserService(IUserRepository users, ILendingRepository lendings, ILogger<UserService>? logger = null)
        {
            _users = users;
            _lendings = lendings;
            _log = new StructuredLogger<UserService>(logger);
        }

        public Task<User?> GetByIdAsync(int userId, CancellationToken ct = default) =>
            GetByIdInternal(userId, ct);

        public Task<IEnumerable<User>> GetMostActiveUsersAsync(string fromRaw, string toRaw, CancellationToken ct = default) =>
            GetMostActiveUsersRawInternal(fromRaw, toRaw, ct);

        public Task<IEnumerable<User>> GetMostActiveUsersAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
            GetMostActiveUsersRangeInternal(from, to, ct);

        private async Task<User?> GetByIdInternal(int userId, CancellationToken ct)
        {
            if (userId <= 0) throw new ValidationException($"UserId must be positive. Provided: {userId}");
            _log.Info("Fetching user by id", new { userId });
            var user = await _users.GetByIdAsync(userId, ct);
            if (user is null)
            {
                _log.Warn("User not found", new { userId });
                return null;
            }
            _log.Info("User retrieved", new { user.Id, user.Email });
            return user;
        }

        private async Task<IEnumerable<User>> GetMostActiveUsersRawInternal(string fromRaw, string toRaw, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fromRaw) || string.IsNullOrWhiteSpace(toRaw))
                throw new ValidationException("From and To must be provided.");
            var from = ParseDateStrict(fromRaw, "From");
            var to = ParseDateStrict(toRaw, "To");
            return await GetMostActiveUsersRangeInternal(from, to, ct);
        }

        private async Task<IEnumerable<User>> GetMostActiveUsersRangeInternal(DateTime from, DateTime to, CancellationToken ct)
        {
            if (from >= to) throw new ValidationException($"From date must be earlier than To date. Provided: from={from:o}, to={to:o}");
            var span = to - from;
            if (span < MinRange) throw new ValidationException("Requested range is too small to evaluate activity.");
            if (span > MaxRange) throw new ValidationException($"Requested range exceeds maximum of {MaxRange.TotalDays} days.");
            if (to > DateTime.UtcNow.AddMinutes(5)) throw new ValidationException($"To date cannot be significantly in the future. Provided: {to:o}");

            _log.Info("Calculating most active users in range", new { from, to, span = span.TotalDays });
            var lendings = (await _lendings.GetLendingsInRangeAsync(from, to, ct))
                .Where(l => l.UserId > 0)
                .ToList();
            if (!lendings.Any())
            {
                _log.Warn("No lending records in specified range", new { from, to });
                return Enumerable.Empty<User>();
            }

            var activity = lendings
                .GroupBy(l => l.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            if (!activity.Any())
            {
                _log.Warn("No user activity derived from lending records", new { from, to });
                return Enumerable.Empty<User>();
            }

            var userIds = activity.Select(a => a.UserId).ToList();
            var users = await _users.GetByIdsAsync(userIds, ct);
            var userMap = users.ToDictionary(u => u.Id);

            var ordered = new List<User>(activity.Count);
            foreach (var entry in activity)
                if (userMap.TryGetValue(entry.UserId, out var u))
                    ordered.Add(u);

            _log.Info("Most active users computed",
                new
                {
                    RangeFrom = from,
                    RangeTo = to,
                    UserCount = ordered.Count,
                    TopUser = ordered.FirstOrDefault()?.Id,
                    ActivitySample = activity.Take(5)
                });

            return ordered;
        }

        private static DateTime ParseDateStrict(string raw, string label)
        {
            if (!DateTime.TryParse(raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var dt))
                throw new ValidationException($"Invalid {label} date: {raw}");
            return dt;
        }
    }
}