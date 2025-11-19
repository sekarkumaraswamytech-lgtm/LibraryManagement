using System.Net;
using System.Net.Http.Json;
using LibrarySystem.gRpcBooks;
using LibrarySystem.gRpcLending;
using LibrarySystem.gRpcUsers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Empty = LibrarySystem.gRpcBooks.Empty;

// IMPORTANT: Use the API project's Program, not a gRPC service Program.
// Ensure LibrarySystemWeb.API project has: public partial class Program { } at end of Program.cs.
namespace LibrarySystem.Tests.Tests.ApiTests
{
    public class ApiEndToEndTests : IClassFixture<WebApplicationFactory<LibrarySystemWeb.API.Program>>
    {
        private readonly WebApplicationFactory<LibrarySystemWeb.API.Program> _factory;

        public ApiEndToEndTests(WebApplicationFactory<LibrarySystemWeb.API.Program> baseFactory)
        {
            _factory = baseFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing gRPC client registrations (if any)
                    var registrations = services.Where(d =>
                        d.ServiceType == typeof(UsersService.UsersServiceClient) ||
                        d.ServiceType == typeof(BookService.BookServiceClient) ||
                        d.ServiceType == typeof(LendingDetailsService.LendingDetailsServiceClient)).ToList();

                    foreach (var r in registrations)
                        services.Remove(r);

                    // Mock Users client
                    var usersClientMock = new Mock<UsersService.UsersServiceClient>();
                    usersClientMock
                        .Setup(c => c.GetMostActiveUsersAsync(It.IsAny<DateRangeRequest>(), null, null, It.IsAny<CancellationToken>()))
                        .Returns(new Grpc.Core.AsyncUnaryCall<UsersResponse>(
                            Task.FromResult(new UsersResponse
                            {
                                Users = { new User { Id = 1, Name = "Alice" } }
                            }),
                            Task.FromResult(new Grpc.Core.Metadata()),
                            () => Grpc.Core.Status.DefaultSuccess,
                            () => new Grpc.Core.Metadata(),
                            () => { }));

                    // Mock Books client
                    var booksClientMock = new Mock<BookService.BookServiceClient>();
                    booksClientMock
                        .Setup(c => c.GetMostBorrowedBooksAsync(It.IsAny<Empty>(), null, null, It.IsAny<CancellationToken>()))
                        .Returns(new Grpc.Core.AsyncUnaryCall<gRpcBooks.BooksResponse>(
                            Task.FromResult(new gRpcBooks.BooksResponse
                            {
                                Books =
                                {
                                    new gRpcBooks.Book { Id = 10, Title = "Clean Code", Author = "Unk", Pages = 100 }
                                }
                            }),
                            Task.FromResult(new Grpc.Core.Metadata()),
                            () => Grpc.Core.Status.DefaultSuccess,
                            () => new Grpc.Core.Metadata(),
                            () => { }));

                    booksClientMock
                        .Setup(c => c.GetReadingPaceAsync(It.IsAny<ReadingPaceRequest>(), null, null, It.IsAny<CancellationToken>()))
                        .Returns(new Grpc.Core.AsyncUnaryCall<ReadingPaceResponse>(
                            Task.FromResult(new ReadingPaceResponse { Pace = 3.5 }),
                            Task.FromResult(new Grpc.Core.Metadata()),
                            () => Grpc.Core.Status.DefaultSuccess,
                            () => new Grpc.Core.Metadata(),
                            () => { }));

                    // Mock Lending client
                    var lendingClientMock = new Mock<LendingDetailsService.LendingDetailsServiceClient>();
                    lendingClientMock
                        .Setup(c => c.GetRelatedBooksAsync(It.IsAny<BookRequest>(), null, null, It.IsAny<CancellationToken>()))
                        .Returns(new Grpc.Core.AsyncUnaryCall<gRpcLending.BooksResponse>(
                            Task.FromResult(new gRpcLending.BooksResponse
                            {
                                Books =
                                {
                                    new gRpcLending.Book { Id = 11, Title = "Refactoring", Author = "Unk", Pages = 200 }
                                }
                            }),
                            Task.FromResult(new Grpc.Core.Metadata()),
                            () => Grpc.Core.Status.DefaultSuccess,
                            () => new Grpc.Core.Metadata(),
                            () => { }));

                    services.AddSingleton(usersClientMock.Object);
                    services.AddSingleton(booksClientMock.Object);
                    services.AddSingleton(lendingClientMock.Object);
                });
            });
        }

        [Fact]
        public async Task Flow_MostBorrowed_Related_ReadingPace_Works()
        {
            var client = _factory.CreateClient();

            // 1. Most borrowed
            var mostBorrowed = await client.GetAsync("/api/library/books/most-borrowed");
            mostBorrowed.StatusCode.Should().Be(HttpStatusCode.OK);

            var mostBorrowedData = await mostBorrowed.Content.ReadFromJsonAsync<List<BookDto>>();
            mostBorrowedData.Should().NotBeNull();
            mostBorrowedData![0].Id.Should().Be(10);

            // 2. Related books
            var related = await client.GetAsync("/api/library/books/10/related");
            related.StatusCode.Should().Be(HttpStatusCode.OK);
            var relatedData = await related.Content.ReadFromJsonAsync<List<BookDto>>();
            relatedData![0].Id.Should().Be(11);

            // 3. Reading pace
            var pace = await client.GetAsync("/api/library/books/1/10/reading-pace");
            pace.StatusCode.Should().Be(HttpStatusCode.OK);
            var paceData = await pace.Content.ReadFromJsonAsync<ReadingPaceDto>();
            paceData!.EstimatedHours.Should().Be(3.5);
        }

        private sealed record BookDto(int Id, string Title, string Author, int Pages);
        private sealed record ReadingPaceDto(int UserId, int BookId, double EstimatedHours);
    }
}