using Grpc.Core;
using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Middleware;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.gRpcUsers.Services
{
    public class UsersgRpcService : UsersService.UsersServiceBase
    {
        private readonly IUserService _userService;
        private readonly IStructuredLogger _log;
        private static readonly TimeSpan DefaultRange = TimeSpan.FromDays(30);

        public UsersgRpcService(IUserService userService, ILogger<UsersgRpcService> logger)
        {
            _userService = userService;
            _log = new StructuredLogger<UsersgRpcService>(logger);
        }

        public override async Task<UsersResponse> GetMostActiveUsers(DateRangeRequest request, ServerCallContext context)
        {
            _log.Info("GetMostActiveUsers invoked", new { request.From, request.To });

            string from = request.From;
            string to = request.To;

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                var end = DateTime.UtcNow;
                var start = end - DefaultRange;
                from = start.ToString("o");
                to = end.ToString("o");
                _log.Warn("Using default date range fallback", new { from, to });
            }

            // Call overload with optional ct parameter (now defined with default so legacy two-arg calls compile elsewhere)
            var users = await _userService.GetMostActiveUsersAsync(from, to, context.CancellationToken);

            var resp = new UsersResponse();
            foreach (var u in users)
            {
                resp.Users.Add(new User { Id = u.Id, Name = u.Name });
            }
            return resp;
        }
    }
}