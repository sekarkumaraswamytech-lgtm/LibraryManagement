using Grpc.Net.Client;
using LibrarySystem.gRpcBooks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LibrarySystem.Tests.Tests.IntegrationTests
{
    // Integration test: real in-process gRPC server (Books) + client.
    public class BooksGrpcIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public BooksGrpcIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAllBooks_ReturnsList()
        {
            using var channel = GrpcChannel.ForAddress(_factory.Server.BaseAddress, new GrpcChannelOptions
            {
                HttpClient = _factory.CreateClient()
            });

            var client = new BookService.BookServiceClient(channel);

            var response = await client.GetAllBooksAsync(new Empty());

            response.Should().NotBeNull();
            response.Books.Should().NotBeNull();
            // Content depends on underlying DB seeding (may be empty if no seed configured).
        }
    }
}