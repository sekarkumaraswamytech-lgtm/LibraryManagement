using LibrarySystem.gRpcBooks;
using LibrarySystem.gRpcLending;
using LibrarySystem.gRpcUsers;
using LibrarySystemWeb.API.Models;
using Microsoft.AspNetCore.Mvc;
using Empty = LibrarySystem.gRpcBooks.Empty;

namespace LibrarySystemWeb.API.Controllers
{
    /// <summary>
    /// Public HTTP facade for library operations.
    /// Responsibility: translate HTTP requests (validated DTOs) into gRPC calls.
    /// No business logic or persistence code lives here.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LibraryController : ControllerBase
    {
        // gRPC clients injected via DI; connect to internal service layer hosts.
        private readonly UsersService.UsersServiceClient _usersClient;
        private readonly BookService.BookServiceClient _booksClient;
        private readonly LendingDetailsService.LendingDetailsServiceClient _lendingClient;

        /// <summary>
        /// Initializes the controller with typed gRPC clients.
        /// </summary>
        /// <param name="usersClient">Client for user activity queries.</param>
        /// <param name="booksClient">Client for book related operations.</param>
        /// <param name="lendingClient">Client for lending / related books operations.</param>
        public LibraryController(
            UsersService.UsersServiceClient usersClient,
            BookService.BookServiceClient booksClient,
            LendingDetailsService.LendingDetailsServiceClient lendingClient)
        {
            _usersClient = usersClient;
            _booksClient = booksClient;
            _lendingClient = lendingClient;
        }

        /// <summary>
        /// Returns most active users within a date range.
        /// Validation is applied to <see cref="MostActiveUsersQuery"/> via FluentValidation before execution.
        /// Empty string fallback allows downstream service to handle / re-validate edge conditions.
        /// </summary>
        /// <param name="query">Query DTO containing 'from' and 'to' date strings.</param>
        /// <param name="ct">Cancellation token propagated to gRPC call.</param>
        /// <returns>Collection of user projections (Id, Name).</returns>
        [HttpGet("users/most-active")]
        public async Task<IActionResult> GetMostActiveUsers([FromQuery] MostActiveUsersQuery query, CancellationToken ct)
        {
            var response = await _usersClient.GetMostActiveUsersAsync(new DateRangeRequest
            {
                From = query.From ?? string.Empty,
                To = query.To ?? string.Empty
            }, cancellationToken: ct);

            // Map proto users to anonymous objects (could be replaced with explicit DTOs)
            return Ok(response.Users.Select(u => new { u.Id, u.Name }));
        }

        /// <summary>
        /// Retrieves the most borrowed book(s). Underlying gRPC service defines selection policy (default top = 1).
        /// </summary>
        /// <param name="ct">Cancellation token for request abort.</param>
        /// <returns>Collection of book projections (Id, Title, Author, Pages).</returns>
        [HttpGet("books/most-borrowed")]
        public async Task<IActionResult> GetMostBorrowedBooks(CancellationToken ct)
        {
            var response = await _booksClient.GetMostBorrowedBooksAsync(new Empty(), cancellationToken: ct);
            return Ok(response.Books.Select(b => new { b.Id, b.Title, b.Author, b.Pages }));
        }

        /// <summary>
        /// Returns books related to a specified book based on shared borrowing patterns by users.
        /// <see cref="RelatedBooksRoute"/> is validated prior to method execution.
        /// </summary>
        /// <param name="route">Route DTO containing the target BookId.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Collection of related books (Id, Title, Author, Pages).</returns>
        [HttpGet("books/{bookId:int}/related")]
        public async Task<IActionResult> GetRelatedBooks(int bookId, CancellationToken ct)
        {
            var response = await _lendingClient.GetRelatedBooksAsync(new BookRequest { BookId = bookId }, cancellationToken: ct);
            return Ok(response.Books.Select(b => new { b.Id, b.Title, b.Author, b.Pages }));
        }

        /// <summary>
        /// Gets estimated reading pace (in hours to finish) for a specific user and book.
        /// Downstream service performs pace calculation using historical lending data.
        /// </summary>
        /// <param name="route">Route DTO containing UserId and BookId.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Object with UserId, BookId, and EstimatedHours.</returns>
        [HttpGet("books/{userId:int}/{bookId:int}/reading-pace")]
        public async Task<IActionResult> GetReadingPace(int userId, int bookId, CancellationToken ct)
        {
            var response = await _booksClient.GetReadingPaceAsync(new ReadingPaceRequest
            {
                UserId = userId,
                BookId = bookId
            }, cancellationToken: ct);

            // Single result projection (could wrap in dedicated DTO for consistency)
            return Ok(new { UserId = userId, BookId = bookId, EstimatedHours = response.Pace });
        }
    }
}
