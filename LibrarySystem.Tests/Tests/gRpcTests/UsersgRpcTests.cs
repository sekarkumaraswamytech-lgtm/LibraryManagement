using Grpc.Net.Client;
using LibrarySystem.gRpcUsers;
using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Middleware;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FluentAssertions;
using Xunit;
using System.Threading;

namespace LibrarySystem.Tests.Tests.gRpcTests
{
    public class UsersgRpcTests : IClassFixture<WebApplicationFactory<LibrarySystem.gRpcUsers.Program>>
    {
        private readonly WebApplicationFactory<LibrarySystem.gRpcUsers.Program> _baseFactory;

        public UsersgRpcTests(WebApplicationFactory<LibrarySystem.gRpcUsers.Program> factory) => _baseFactory = factory;

        private WebApplicationFactory<LibrarySystem.gRpcUsers.Program> WithMock(Action<Mock<IUserService>> cfg)
        {
            return _baseFactory.WithWebHostBuilder(b =>
            {
                b.ConfigureServices(s =>
                {
                    var existing = s.FirstOrDefault(d => d.ServiceType == typeof(IUserService));
                    if (existing != null) s.Remove(existing);
                    var mock = new Mock<IUserService>();
                    cfg(mock);
                    s.AddSingleton(mock.Object);
                });
            });
        }

        [Fact]
        public async Task MostActiveUsers_Success()
        {
            var factory = WithMock(m =>
            {
                m.Setup(x => x.GetMostActiveUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[]
                 {
                     new LibrarySystem.Domain.Models.User { Id = 1, Name = "Alice", Email="alice@test.local"},
                     new LibrarySystem.Domain.Models.User { Id = 2, Name = "Bob", Email="bob@test.local"}
                 });
            });

            using var channel = GrpcChannel.ForAddress(factory.Server.BaseAddress, new GrpcChannelOptions { HttpClient = factory.CreateClient() });
            var client = new UsersService.UsersServiceClient(channel);

            var resp = await client.GetMostActiveUsersAsync(new DateRangeRequest { From = "2024-01-01", To = "2024-02-01" });
            resp.Users.Should().HaveCount(2);
        }

        [Fact]
        public async Task MostActiveUsers_InvalidDates_ReturnsInvalidArgument()
        {
            var factory = WithMock(m =>
            {
                m.Setup(x => x.GetMostActiveUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .Throws(new ValidationException("Invalid dates"));
            });

            using var channel = GrpcChannel.ForAddress(factory.Server.BaseAddress, new GrpcChannelOptions { HttpClient = factory.CreateClient() });
            var client = new UsersService.UsersServiceClient(channel);

            var act = async () => await client.GetMostActiveUsersAsync(new DateRangeRequest { From = "", To = "" });
            var ex = await Assert.ThrowsAsync<Grpc.Core.RpcException>(act);
            ex.StatusCode.Should().Be(Grpc.Core.StatusCode.InvalidArgument);
        }
    }
}