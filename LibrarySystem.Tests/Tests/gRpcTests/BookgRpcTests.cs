using Grpc.Core;
using Grpc.Net.Client;
using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Middleware;
using LibrarySystem.gRpcBooks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LibrarySystem.Tests.Tests.gRpcTests
{
    // gRPC integration tests for Book service (similar pattern to UsersgRpcTests).
    public class BookgRpcTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _baseFactory;

        public BookgRpcTests(WebApplicationFactory<Program> factory)
        {
            _baseFactory = factory;
        }

        private WebApplicationFactory<Program> WithMock(Action<Mock<IBookService>> configure)
        {
            return _baseFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing IBookService registration (if any)
                    var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IBookService));
                    if (existing != null) services.Remove(existing);

                    var mock = new Mock<IBookService>();

                    // Provide benign defaults
                    mock.Setup(m => m.GetAllAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Enumerable.Empty<LibrarySystem.Domain.Models.Book>());
                    mock.Setup(m => m.GetMostBorrowedBooksAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Enumerable.Empty<LibrarySystem.Domain.Models.Book>());
                    mock.Setup(m => m.EstimateReadingPaceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(1d);

                    configure(mock);
                    services.AddSingleton(mock.Object);
                });
            });
        }

        private BookService.BookServiceClient CreateClient(WebApplicationFactory<Program> factory)
        {
            var channel = GrpcChannel.ForAddress(factory.Server.BaseAddress, new GrpcChannelOptions
            {
                HttpClient = factory.CreateClient()
            });
            return new BookService.BookServiceClient(channel);
        }

        [Fact]
        public async Task GetAllBooks_ReturnsMappedBooks()
        {
            var factory = WithMock(m =>
            {
                m.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[]
                 {
                     new LibrarySystem.Domain.Models.Book { Id = 1, Title = "Clean Code", Author = "Robert C. Martin", Pages = 464 }
                 });
            });

            var client = CreateClient(factory);
            var response = await client.GetAllBooksAsync(new Empty());

            response.Should().NotBeNull();
            response.Books.Should().HaveCount(1);
            response.Books[0].Title.Should().Be("Clean Code");
        }

        [Fact]
        public async Task GetMostBorrowedBooks_ReturnsTopBooks()
        {
            var factory = WithMock(m =>
            {
                m.Setup(s => s.GetMostBorrowedBooksAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[]
                 {
                     new LibrarySystem.Domain.Models.Book { Id = 2, Title = "Refactoring", Author = "Martin Fowler", Pages = 448 },
                     new LibrarySystem.Domain.Models.Book { Id = 3, Title = "Patterns", Author = "GoF", Pages = 395 }
                 });
            });

            var client = CreateClient(factory);
            var response = await client.GetMostBorrowedBooksAsync(new Empty());

            response.Books.Should().HaveCount(2);
            response.Books.Select(b => b.Id).Should().BeEquivalentTo(new[] { 2, 3 });
        }

        [Fact]
        public async Task GetReadingPace_ReturnsValue()
        {
            var factory = WithMock(m =>
            {
                m.Setup(s => s.EstimateReadingPaceAsync(10, 100, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(12.75);
            });

            var client = CreateClient(factory);
            var resp = await client.GetReadingPaceAsync(new ReadingPaceRequest { UserId = 10, BookId = 100 });

            resp.Pace.Should().Be(12.75);
        }

        [Fact]
        public async Task GetReadingPace_InvalidIds_ReturnsInvalidArgument()
        {
            var factory = WithMock(m =>
            {
                m.Setup(s => s.EstimateReadingPaceAsync(It.Is<int>(u => u <= 0), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .Throws(new ValidationException("UserId must be positive."));
            });

            var client = CreateClient(factory);

            var act = async () => await client.GetReadingPaceAsync(new ReadingPaceRequest { UserId = 0, BookId = 50 });

            var ex = await Assert.ThrowsAsync<RpcException>(act);
            ex.StatusCode.Should().Be(StatusCode.InvalidArgument);
            ex.Status.Detail.Should().Contain("UserId must be positive");
        }
    }
}
