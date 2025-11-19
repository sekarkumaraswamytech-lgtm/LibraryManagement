using Grpc.Core;
using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Middleware;

namespace LibrarySystem.gRpcLending.Services
{
    public class LendingGrpcService : LendingDetailsService.LendingDetailsServiceBase
    {
        private readonly ILendingService _lendingService;
        private readonly IStructuredLogger _log;

        public LendingGrpcService(ILendingService lendingService, ILogger<LendingGrpcService> logger)
        {
            _lendingService = lendingService;
            _log = new StructuredLogger<LendingGrpcService>(logger);
        }

        public override async Task<BooksResponse> GetRelatedBooks(BookRequest request, ServerCallContext context)
        {
            _log.Info("GetRelatedBooks invoked", new { request.BookId });
            var related = await _lendingService.GetRelatedBooksAsync(request.BookId, context.CancellationToken);
            var response = new BooksResponse();
            foreach (var b in related)
            {
                response.Books.Add(new Book { Id = b.Id, Title = b.Title, Author = b.Author, Pages = b.Pages });
            }
            return response;
        }

        public override async Task<Empty> RecordBorrow(BorrowRequest request, ServerCallContext context)
        {
            _log.Info("RecordBorrow invoked", new { request.UserId, request.BookId });
            await _lendingService.RecordBorrowAsync(request.UserId, request.BookId, context.CancellationToken);
            _log.Info("RecordBorrow completed");
            return new Empty();
        }

        public override async Task<Empty> RecordReturn(ReturnRequest request, ServerCallContext context)
        {
            _log.Info("RecordReturn invoked", new { request.LendingId });
            await _lendingService.RecordReturnAsync(request.LendingId, context.CancellationToken);
            _log.Info("RecordReturn completed");
            return new Empty();
        }
    }
}