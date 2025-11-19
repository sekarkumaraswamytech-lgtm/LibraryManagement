using Grpc.Core;
using Grpc.Net.Client;
using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Middleware;
using LibrarySystem.gRpcLending;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FluentAssertions;

namespace LibrarySystem.Tests.Tests.gRpcTests
{
    // Integration tests for Lending gRPC service using in-process host with mocked ILendingService.
    public class LendinggRpcTests : IClassFixture<WebApplicationFactory<LibrarySystem.gRpcLending.Program>>
    {
        private readonly WebApplicationFactory<LibrarySystem.gRpcLending.Program> _baseFactory;

        public LendinggRpcTests(WebApplicationFactory<LibrarySystem.gRpcLending.Program> factory)
        {
            _baseFactory = factory;
        }

        private WebApplicationFactory<LibrarySystem.gRpcLending.Program> WithMockService(
            Action<Mock<ILendingService>> configure)
        {
            return _baseFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var existing = services.FirstOrDefault(d => d.ServiceType == typeof(ILendingService));
                    if (existing != null) services.Remove(existing);

                    var mock = new Mock<ILendingService>();

                    // Default no-op implementations (can be overridden by configure)
                    mock.Setup(m => m.GetRelatedBooksAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Array.Empty<LibrarySystem.Domain.Models.Book>());
                    mock.Setup(m => m.RecordBorrowAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
                    mock.Setup(m => m.RecordReturnAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

                    configure(mock);
                    services.AddSingleton(mock.Object);
                });
            });
        }

        private static LendingDetailsService.LendingDetailsServiceClient CreateClient(WebApplicationFactory<LibrarySystem.gRpcLending.Program> factory)
        {
            var channel = GrpcChannel.ForAddress(factory.Server.BaseAddress, new GrpcChannelOptions
            {
                HttpClient = factory.CreateClient()
            });
            return new LendingDetailsService.LendingDetailsServiceClient(channel);
        }

        [Fact]
        public async Task GetRelatedBooks_ReturnsMappedBooks()
        {
            var factory = WithMockService(m =>
            {
                m.Setup(s => s.GetRelatedBooksAsync(10, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[]
                    {
                        new LibrarySystem.Domain.Models.Book { Id = 2, Title = "Refactoring", Author = "Fowler", Pages = 448 },
                        new LibrarySystem.Domain.Models.Book { Id = 3, Title = "Patterns", Author = "GoF", Pages = 395 }
                    });
            });

            var client = CreateClient(factory);

            var response = await client.GetRelatedBooksAsync(new BookRequest { BookId = 10 });

            response.Books.Should().HaveCount(2);
            response.Books.Select(b => b.Id).Should().BeEquivalentTo(new[] { 2, 3 });
        }

        [Fact]
        public async Task RecordBorrow_Succeeds()
        {
            var tcs = new TaskCompletionSource<bool>();
            var factory = WithMockService(m =>
            {
                m.Setup(s => s.RecordBorrowAsync(1, 10, It.IsAny<CancellationToken>()))
                    .Callback(() => tcs.SetResult(true))
                    .Returns(Task.CompletedTask);
            });

            var client = CreateClient(factory);

            var result = await client.RecordBorrowAsync(new BorrowRequest { UserId = 1, BookId = 10 });

            result.Should().NotBeNull();
            (await tcs.Task).Should().BeTrue();
        }

        [Fact]
        public async Task RecordReturn_Succeeds()
        {
            var tcs = new TaskCompletionSource<bool>();
            var factory = WithMockService(m =>
            {
                m.Setup(s => s.RecordReturnAsync(55, It.IsAny<CancellationToken>()))
                    .Callback(() => tcs.SetResult(true))
                    .Returns(Task.CompletedTask);
            });

            var client = CreateClient(factory);

            var result = await client.RecordReturnAsync(new ReturnRequest { LendingId = 55 });

            result.Should().NotBeNull();
            (await tcs.Task).Should().BeTrue();
        }

        [Fact]
        public async Task GetRelatedBooks_InvalidId_ReturnsInvalidArgument()
        {
            var factory = WithMockService(m =>
            {
                m.Setup(s => s.GetRelatedBooksAsync(It.Is<int>(id => id <= 0), It.IsAny<CancellationToken>()))
                    .Throws(new ValidationException("BookId must be positive."));
            });

            var client = CreateClient(factory);

            var act = async () => await client.GetRelatedBooksAsync(new BookRequest { BookId = 0 });

            var ex = await Assert.ThrowsAsync<RpcException>(act);
            ex.StatusCode.Should().Be(StatusCode.InvalidArgument);
            ex.Status.Detail.Should().Contain("BookId must be positive");
        }

        [Fact]
        public async Task RecordBorrow_ValidationFailure_ReturnsInvalidArgument()
        {
            var factory = WithMockService(m =>
            {
                m.Setup(s => s.RecordBorrowAsync(It.IsAny<int>(), It.Is<int>(b => b <= 0), It.IsAny<CancellationToken>()))
                    .Throws(new ValidationException("Invalid BookId"));
            }); 

            var client = CreateClient(factory);

            var act = async () => await client.RecordBorrowAsync(new BorrowRequest { UserId = 1, BookId = 0 });

            var ex = await Assert.ThrowsAsync<RpcException>(act);
            ex.StatusCode.Should().Be(StatusCode.InvalidArgument);
            ex.Status.Detail.Should().Contain("Invalid BookId");
        }

        [Fact]
        public async Task RecordReturn_NotFound_ReturnsNotFound()
        {
            var factory = WithMockService(m =>
            {
                m.Setup(s => s.RecordReturnAsync(999, It.IsAny<CancellationToken>()))
                    .Throws(new NotFoundException("Lending record not found."));
            });

            var client = CreateClient(factory);

            var act = async () => await client.RecordReturnAsync(new ReturnRequest { LendingId = 999 });

            var ex = await Assert.ThrowsAsync<RpcException>(act);
            ex.StatusCode.Should().Be(StatusCode.NotFound);
            ex.Status.Detail.Should().Contain("not found");
        }
    }
}
