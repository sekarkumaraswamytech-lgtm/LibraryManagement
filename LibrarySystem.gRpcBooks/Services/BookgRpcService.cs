using Grpc.Core;
using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Middleware;

namespace LibrarySystem.gRpcBooks.Services
{
    public class BookgRpcService : BookService.BookServiceBase
    {
        private readonly IBookService _bookService;
        private readonly IStructuredLogger _log;

        public BookgRpcService(IBookService bookService, ILogger<BookgRpcService> logger)
        {
            _bookService = bookService;
            _log = new StructuredLogger<BookgRpcService>(logger);
        }

        public override async Task<BooksResponse> GetMostBorrowedBooks(Empty request, ServerCallContext context)
        {
            _log.Info("GetMostBorrowedBooks invoked");
            var books = await _bookService.GetMostBorrowedBooksAsync(context.CancellationToken);
            var response = new BooksResponse();
            foreach (var b in books)
            {
                response.Books.Add(new Book { Id = b.Id, Title = b.Title, Author = b.Author, Pages = b.Pages });
            }
            return response;
        }

        public override async Task<BooksResponse> GetAllBooks(Empty request, ServerCallContext context)
        {
            _log.Info("GetAllBooks invoked");
            var books = await _bookService.GetAllAsync(context.CancellationToken);
            var response = new BooksResponse();
            foreach (var b in books)
            {
                response.Books.Add(new Book { Id = b.Id, Title = b.Title, Author = b.Author, Pages = b.Pages });
            }
            return response;
        }

        public override async Task<ReadingPaceResponse> GetReadingPace(ReadingPaceRequest request, ServerCallContext context)
        {
            _log.Info("GetReadingPace invoked", new { request.UserId, request.BookId });
            var pace = await _bookService.EstimateReadingPaceAsync(request.UserId, request.BookId, context.CancellationToken);
            return new ReadingPaceResponse { Pace = pace };
        }
    }
}
